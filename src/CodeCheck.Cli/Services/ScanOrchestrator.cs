using CodeCheck.Core.Configuration;
using CodeCheck.Core.Build;
using CodeCheck.Core.Baseline;
using CodeCheck.Core.Engines;
using CodeCheck.Core.Inputs;
using CodeCheck.Core.Issues;
using CodeCheck.Core.Quality;
using CodeCheck.Core.Reports;
using CodeCheck.Core.Rules;
using CodeCheck.Core.Suppression;

namespace CodeCheck.Cli.Services;

public sealed class ScanOrchestrator
{
    private readonly ConfigLoader _configLoader = new();
    private readonly ConfigValidator _configValidator = new();
    private readonly FileDiscoveryService _fileDiscoveryService = new();
    private readonly CompileContextBuilder _compileContextBuilder = new();
    private readonly FingerprintService _fingerprintService = new();
    private readonly SuppressionService _suppressionService = new();
    private readonly BaselineService _baselineService = new();
    private readonly QualityScoreService _qualityScoreService = new();
    private readonly SummaryService _summaryService = new();
    private readonly LizardRunner _lizardRunner = new();
    private readonly CppcheckRunner _cppcheckRunner = new();
    private readonly ClangTidyRunner _clangTidyRunner = new();
    private readonly BuiltinRuleRunner _builtinRuleRunner = new();
    private readonly RuleLoader _ruleLoader = new();
    private readonly ReportGenerationService _reportGenerationService = new();
    private readonly ControlFileService _controlFileService = new();
    private readonly ScanStatusService _scanStatusService = new();
    private readonly CliProgressReporter _progressReporter = new();

    public async Task<int> ValidateAsync(string configPath, CancellationToken cancellationToken)
    {
        if (!File.Exists(configPath))
        {
            Console.Error.WriteLine($"Config file not found: {configPath}");
            return 2;
        }

        var config = await _configLoader.LoadAsync(configPath, cancellationToken);
        var result = _configValidator.Validate(config, configPath, isScan: false);
        if (result.IsValid)
        {
            await ValidateRulesAsync(config, result, cancellationToken);
        }

        WriteValidationResult(result);
        return result.IsValid ? 0 : 2;
    }

    public async Task<int> ScanAsync(string configPath, CancellationToken cancellationToken)
    {
        var startedAt = DateTime.Now;
        _progressReporter.Write("scan-started", new { config = configPath });

        if (!File.Exists(configPath))
        {
            Console.Error.WriteLine($"Config file not found: {configPath}");
            return 2;
        }

        var config = await _configLoader.LoadAsync(configPath, cancellationToken);
        await _scanStatusService.WriteAsync(config, Directory.GetCurrentDirectory(), new ScanStatus { Status = "Running", Phase = "Validating", Message = "Validating configuration." }, cancellationToken);
        var validation = _configValidator.Validate(config, configPath, isScan: true);
        if (validation.IsValid)
        {
            await ValidateRulesAsync(config, validation, cancellationToken);
        }

        if (!validation.IsValid)
        {
            WriteValidationResult(validation);
            return 2;
        }

        var files = await _fileDiscoveryService.DiscoverAsync(config, cancellationToken);
        var compileContext = _compileContextBuilder.Build(config, files, Directory.GetCurrentDirectory());
        _progressReporter.Write("files-discovered", new { total = files.Count, scheduled = files.Count, excluded = 0 });
        await _scanStatusService.WriteAsync(config, Directory.GetCurrentDirectory(), new ScanStatus { Status = "Running", Phase = "FilesDiscovered", TotalFiles = files.Count, Message = "Files discovered." }, cancellationToken);
        if (await _controlFileService.WaitIfPausedAsync(config, Directory.GetCurrentDirectory(), cancellationToken))
        {
            return await WriteCancelledReportAsync(config, startedAt, files, [], [], cancellationToken);
        }

        _progressReporter.Write("engine-started", new { engine = "lizard", fileCount = files.Count });
        await _scanStatusService.WriteAsync(config, Directory.GetCurrentDirectory(), new ScanStatus { Status = "Running", Phase = "EngineRunning", Engine = "lizard", TotalFiles = files.Count, Message = "Running lizard." }, cancellationToken);
        var lizardResult = await _lizardRunner.AnalyzeAsync(config, compileContext, files, cancellationToken);
        _progressReporter.Write("engine-completed", new { engine = "lizard", issues = lizardResult.Issues.Count, failedFiles = lizardResult.FailedFiles.Count });
        await _scanStatusService.WriteAsync(config, Directory.GetCurrentDirectory(), new ScanStatus { Status = "Running", Phase = "EngineCompleted", Engine = "lizard", TotalFiles = files.Count, TotalIssues = lizardResult.Issues.Count, FailedFiles = lizardResult.FailedFiles.Count, Message = "lizard completed." }, cancellationToken);
        if (await _controlFileService.WaitIfPausedAsync(config, Directory.GetCurrentDirectory(), cancellationToken))
        {
            return await WriteCancelledReportAsync(config, startedAt, files, lizardResult.Issues.ToList(), lizardResult.FailedFiles.ToList(), cancellationToken);
        }

        _progressReporter.Write("engine-started", new { engine = "cppcheck", fileCount = files.Count });
        await _scanStatusService.WriteAsync(config, Directory.GetCurrentDirectory(), new ScanStatus { Status = "Running", Phase = "EngineRunning", Engine = "cppcheck", TotalFiles = files.Count, TotalIssues = lizardResult.Issues.Count, FailedFiles = lizardResult.FailedFiles.Count, Message = "Running cppcheck." }, cancellationToken);
        var cppcheckResult = await _cppcheckRunner.AnalyzeAsync(config, compileContext, files, cancellationToken);
        _progressReporter.Write("engine-completed", new { engine = "cppcheck", issues = cppcheckResult.Issues.Count, failedFiles = cppcheckResult.FailedFiles.Count });
        await _scanStatusService.WriteAsync(config, Directory.GetCurrentDirectory(), new ScanStatus { Status = "Running", Phase = "EngineCompleted", Engine = "cppcheck", TotalFiles = files.Count, TotalIssues = lizardResult.Issues.Count + cppcheckResult.Issues.Count, FailedFiles = lizardResult.FailedFiles.Count + cppcheckResult.FailedFiles.Count, Message = "cppcheck completed." }, cancellationToken);
        if (await _controlFileService.WaitIfPausedAsync(config, Directory.GetCurrentDirectory(), cancellationToken))
        {
            var partialIssues = lizardResult.Issues.Concat(cppcheckResult.Issues).ToList();
            var partialFailedFiles = lizardResult.FailedFiles.Concat(cppcheckResult.FailedFiles).ToList();
            return await WriteCancelledReportAsync(config, startedAt, files, partialIssues, partialFailedFiles, cancellationToken);
        }

        _progressReporter.Write("engine-started", new { engine = "clang-tidy", fileCount = files.Count });
        await _scanStatusService.WriteAsync(config, Directory.GetCurrentDirectory(), new ScanStatus { Status = "Running", Phase = "EngineRunning", Engine = "clang-tidy", TotalFiles = files.Count, TotalIssues = lizardResult.Issues.Count + cppcheckResult.Issues.Count, FailedFiles = lizardResult.FailedFiles.Count + cppcheckResult.FailedFiles.Count, Message = "Running clang-tidy." }, cancellationToken);
        var clangTidyResult = await _clangTidyRunner.AnalyzeAsync(config, compileContext, files, cancellationToken);
        _progressReporter.Write("engine-completed", new { engine = "clang-tidy", issues = clangTidyResult.Issues.Count, failedFiles = clangTidyResult.FailedFiles.Count });
        await _scanStatusService.WriteAsync(config, Directory.GetCurrentDirectory(), new ScanStatus { Status = "Running", Phase = "EngineCompleted", Engine = "clang-tidy", TotalFiles = files.Count, TotalIssues = lizardResult.Issues.Count + cppcheckResult.Issues.Count + clangTidyResult.Issues.Count, FailedFiles = lizardResult.FailedFiles.Count + cppcheckResult.FailedFiles.Count + clangTidyResult.FailedFiles.Count, Message = "clang-tidy completed." }, cancellationToken);
        if (await _controlFileService.WaitIfPausedAsync(config, Directory.GetCurrentDirectory(), cancellationToken))
        {
            var partialIssues = lizardResult.Issues.Concat(cppcheckResult.Issues).Concat(clangTidyResult.Issues).ToList();
            var partialFailedFiles = lizardResult.FailedFiles.Concat(cppcheckResult.FailedFiles).Concat(clangTidyResult.FailedFiles).ToList();
            return await WriteCancelledReportAsync(config, startedAt, files, partialIssues, partialFailedFiles, cancellationToken);
        }

        _progressReporter.Write("engine-started", new { engine = "CodeCheckBuiltin", fileCount = files.Count });
        await _scanStatusService.WriteAsync(config, Directory.GetCurrentDirectory(), new ScanStatus { Status = "Running", Phase = "EngineRunning", Engine = "CodeCheckBuiltin", TotalFiles = files.Count, TotalIssues = lizardResult.Issues.Count + cppcheckResult.Issues.Count + clangTidyResult.Issues.Count, FailedFiles = lizardResult.FailedFiles.Count + cppcheckResult.FailedFiles.Count + clangTidyResult.FailedFiles.Count, Message = "Running builtin rules." }, cancellationToken);
        var builtinResult = await _builtinRuleRunner.AnalyzeAsync(config, compileContext, files, cancellationToken);
        var issues = lizardResult.Issues.Concat(cppcheckResult.Issues).Concat(clangTidyResult.Issues).Concat(builtinResult.Issues).ToList();
        var failedFiles = lizardResult.FailedFiles.Concat(cppcheckResult.FailedFiles).Concat(clangTidyResult.FailedFiles).Concat(builtinResult.FailedFiles).ToList();
        _fingerprintService.ApplyFingerprints(issues);
        var suppressionInfo = await _suppressionService.ApplyAsync(issues, config, Directory.GetCurrentDirectory(), cancellationToken);
        var baselineInfo = await _baselineService.ApplyAsync(issues, config, Directory.GetCurrentDirectory(), cancellationToken);
        var qualityScore = _qualityScoreService.Calculate(issues, failedFiles);
        var summary = _summaryService.Build(issues, baselineInfo, suppressionInfo);
        _progressReporter.Write("engine-completed", new { engine = "CodeCheckBuiltin", issues = issues.Count, failedFiles = 0 });

        var finishedAt = DateTime.Now;
        var outputDirectory = CreateRunOutputDirectory(config, startedAt);

        var report = new ScanReport
        {
            ReportId = $"{startedAt:yyyyMMdd-HHmmss}-{GetProjectName(config)}",
            Project = new ProjectInfo { Name = GetProjectName(config), Root = config.Project.Root, ProjectKey = config.Project.ProjectKey },
            Scan = new ScanInfo { StartedAt = startedAt, FinishedAt = finishedAt, Status = failedFiles.Count == 0 ? "Completed" : "CompletedWithFailedFiles", InputType = config.Input.Type, TotalFilesDiscovered = files.Count, TotalFilesScheduled = files.Count, TotalFilesScanned = files.Count, TotalFilesFailed = failedFiles.Count },
            Summary = summary,
            QualityScore = qualityScore,
            Issues = issues.ToList(),
            FailedFiles = failedFiles,
            Baseline = baselineInfo,
            Suppression = suppressionInfo,
            SuppressedIssues = suppressionInfo.SuppressedIssues,
            Logs = [new LogEntry { Level = "Info", Time = finishedAt, Message = $"Scheduled {files.Count} files and found {issues.Count} issues." }]
        };

        await _scanStatusService.WriteAsync(config, Directory.GetCurrentDirectory(), new ScanStatus { Status = "Running", Phase = "ReportGenerating", TotalFiles = files.Count, TotalIssues = issues.Count, FailedFiles = failedFiles.Count, Message = "Generating reports." }, cancellationToken);
        var generatedReports = await _reportGenerationService.GenerateAsync(report, config.Report, outputDirectory, files.Count, finishedAt, cancellationToken);
        await _scanStatusService.WriteAsync(config, Directory.GetCurrentDirectory(), new ScanStatus { Status = report.Scan.Status, Phase = "Completed", TotalFiles = files.Count, TotalIssues = issues.Count, FailedFiles = failedFiles.Count, ReportPath = generatedReports.JsonPath, Message = "Scan completed." }, cancellationToken);

        foreach (var output in report.Outputs.Where(output => !string.Equals(output.Key, "log", StringComparison.OrdinalIgnoreCase)))
        {
            _progressReporter.Write("report-generated", new { format = output.Key, path = output.Value });
        }

        _progressReporter.Write("scan-completed", new { status = report.Scan.Status, totalIssues = issues.Count, report = generatedReports.JsonPath });
        return 0;
    }

    private async Task<int> WriteCancelledReportAsync(CodeCheckConfig config, DateTime startedAt, IReadOnlyList<ScanInputFile> files, List<Issue> issues, List<FailedFile> failedFiles, CancellationToken cancellationToken)
    {
        _fingerprintService.ApplyFingerprints(issues);
        var suppressionInfo = await _suppressionService.ApplyAsync(issues, config, Directory.GetCurrentDirectory(), cancellationToken);
        var baselineInfo = new BaselineInfo { Enabled = config.Baseline.Enabled, State = "NotCompared" };
        var qualityScore = _qualityScoreService.Calculate(issues, failedFiles);
        var summary = _summaryService.Build(issues, baselineInfo, suppressionInfo);
        var finishedAt = DateTime.Now;
        var outputDirectory = CreateRunOutputDirectory(config, startedAt);
        var report = new ScanReport
        {
            ReportId = $"{startedAt:yyyyMMdd-HHmmss}-{GetProjectName(config)}",
            Project = new ProjectInfo { Name = GetProjectName(config), Root = config.Project.Root, ProjectKey = config.Project.ProjectKey },
            Scan = new ScanInfo { StartedAt = startedAt, FinishedAt = finishedAt, Status = "Cancelled", InputType = config.Input.Type, TotalFilesDiscovered = files.Count, TotalFilesScheduled = files.Count, TotalFilesScanned = 0, TotalFilesFailed = failedFiles.Count },
            Summary = summary,
            QualityScore = qualityScore,
            Issues = issues,
            FailedFiles = failedFiles,
            Baseline = baselineInfo,
            Suppression = suppressionInfo,
            SuppressedIssues = suppressionInfo.SuppressedIssues,
            Logs = [new LogEntry { Level = "Warning", Time = finishedAt, Message = "Scan cancelled by control file." }]
        };

        var generatedReports = await _reportGenerationService.GenerateAsync(report, config.Report, outputDirectory, files.Count, finishedAt, cancellationToken);
        await _scanStatusService.WriteAsync(config, Directory.GetCurrentDirectory(), new ScanStatus { Status = "Cancelled", Phase = "Cancelled", TotalFiles = files.Count, TotalIssues = issues.Count, FailedFiles = failedFiles.Count, ReportPath = generatedReports.JsonPath, Message = "Scan cancelled by control file." }, cancellationToken);
        _progressReporter.Write("scan-completed", new { status = "Cancelled", totalIssues = issues.Count, report = generatedReports.JsonPath });
        return 6;
    }

    private async Task ValidateRulesAsync(CodeCheckConfig config, ValidationResult result, CancellationToken cancellationToken)
    {
        try
        {
            var ruleSet = await _ruleLoader.LoadAsync(config.Rules.RuleIndex, cancellationToken);
            var duplicateRuleIds = ruleSet.Rules.GroupBy(rule => rule.Id, StringComparer.OrdinalIgnoreCase).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
            foreach (var duplicateRuleId in duplicateRuleIds)
            {
                result.AddError($"Duplicate rule id: {duplicateRuleId}");
            }

            if (!ruleSet.Profiles.Any(profile => string.Equals(profile.Name, config.Rules.Profile, StringComparison.OrdinalIgnoreCase)))
            {
                result.AddError($"Rule profile not found: {config.Rules.Profile}");
            }

            if (ruleSet.Rules.Count != 100)
            {
                result.AddWarning($"Rule count is {ruleSet.Rules.Count}; target is 100.");
            }
            else
            {
                result.AddWarning("Rule set loaded: 100 rules.");
            }
        }
        catch (Exception ex)
        {
            result.AddError($"Failed to load rules: {ex.Message}");
        }
    }

    private static void WriteValidationResult(ValidationResult result)
    {
        foreach (var warning in result.Warnings) Console.WriteLine($"[Warning] {warning}");
        foreach (var error in result.Errors) Console.Error.WriteLine($"[Error] {error}");
        if (result.IsValid) Console.WriteLine("Config validation passed.");
    }

    private static string CreateRunOutputDirectory(CodeCheckConfig config, DateTime startedAt)
    {
        var directory = Path.Combine(config.Report.OutputDirectory, GetProjectName(config), startedAt.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string GetProjectName(CodeCheckConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.Project.Name)) return config.Project.Name;
        var firstPath = config.Input.Paths.FirstOrDefault();
        return string.IsNullOrWhiteSpace(firstPath) ? "CodeCheckProject" : Path.GetFileNameWithoutExtension(firstPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
    }
}
