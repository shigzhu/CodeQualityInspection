using System.Text.RegularExpressions;
using CodeCheck.Core.Build;
using CodeCheck.Core.Configuration;
using CodeCheck.Core.Inputs;
using CodeCheck.Core.Issues;
using CodeCheck.Core.Reports;
using CodeCheck.Core.Runtime;

namespace CodeCheck.Core.Engines;

public sealed class ClangTidyRunner : IAnalysisEngine
{
    private static readonly Regex DiagnosticPattern = new(@"^(?<file>.+?):(?<line>\d+):(?<column>\d+):\s+(?<level>warning|error):\s+(?<message>.+?)\s+\[(?<check>[^\]]+)\]$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private readonly ExternalProcessRunner _processRunner;

    public ClangTidyRunner()
        : this(new ExternalProcessRunner())
    {
    }

    public ClangTidyRunner(ExternalProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    public string Name => "clang-tidy";

    public async Task<EngineResult> AnalyzeAsync(CodeCheckConfig config, CompileContext compileContext, IReadOnlyList<ScanInputFile> files, CancellationToken cancellationToken)
    {
        var result = new EngineResult { EngineName = Name };
        if (!config.Engines.ClangTidy.Enabled)
        {
            return result;
        }

        if (string.IsNullOrWhiteSpace(config.Engines.ClangTidy.Path) || !File.Exists(config.Engines.ClangTidy.Path))
        {
            result.FailedFiles.Add(new FailedFile { Stage = "EngineRun", ErrorCode = "ClangTidyMissing", Message = "clang-tidy executable not found." });
            return result;
        }

        foreach (var file in files.Where(file => File.Exists(file.FullPath)))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var arguments = BuildArguments(file, compileContext);
            var processResult = await _processRunner.RunAsync(config.Engines.ClangTidy.Path, arguments, Directory.GetCurrentDirectory(), TimeSpan.FromSeconds(120), cancellationToken);
            var output = string.Concat(processResult.StandardOutput, Environment.NewLine, processResult.StandardError);
            var issues = ParseIssues(output, [file]);
            result.Issues.AddRange(issues);

            if (!processResult.Success && issues.Count == 0)
            {
                result.FailedFiles.Add(new FailedFile
                {
                    File = file.FullPath,
                    RelativeFile = file.RelativePath,
                    Stage = "EngineRun",
                    ErrorCode = processResult.TimedOut ? "ClangTidyTimeout" : "ClangTidyFailed",
                    Message = string.IsNullOrWhiteSpace(output) ? "clang-tidy failed." : output.Trim()
                });
            }
        }

        return result;
    }

    public static List<Issue> ParseIssues(string output, IReadOnlyList<ScanInputFile> files)
    {
        var issues = new List<Issue>();
        if (string.IsNullOrWhiteSpace(output))
        {
            return issues;
        }

        var issueIndex = 1;
        foreach (var line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var match = DiagnosticPattern.Match(line.Trim());
            if (!match.Success)
            {
                continue;
            }

            var filePath = match.Groups["file"].Value;
            var file = ResolveFile(filePath, files);
            var engineRuleId = match.Groups["check"].Value;
            var language = file?.Language == "c" ? "c" : "cpp";

            issues.Add(new Issue
            {
                IssueId = $"CLANGTIDY-{issueIndex:D6}",
                RuleId = MapRuleId(engineRuleId, language),
                Severity = match.Groups["level"].Value == "error" ? "Critical" : "Warning",
                Language = language,
                Engine = "clang-tidy",
                Message = match.Groups["message"].Value,
                File = file?.RelativePath ?? filePath.Replace('/', Path.DirectorySeparatorChar),
                Line = int.Parse(match.Groups["line"].Value)
            });
            issueIndex++;
        }

        return issues;
    }

    public static List<string> BuildArguments(ScanInputFile file, CompileContext compileContext)
    {
        var arguments = new List<string> { file.FullPath, "--" };

        if (file.IsHeader)
        {
            arguments.Add(file.Language == "c" ? "-x" : "-x");
            arguments.Add(file.Language == "c" ? "c" : "c++");
        }

        arguments.Add(file.Language == "c" ? $"-std={compileContext.CStandard}" : $"-std={compileContext.CppStandard}");
        arguments.AddRange(compileContext.IncludeDirectories.Select(include => $"-I{include}"));
        arguments.AddRange(compileContext.Defines.Select(define => $"-D{define}"));
        return arguments;
    }

    private static ScanInputFile? ResolveFile(string path, IReadOnlyList<ScanInputFile> files)
    {
        return files.FirstOrDefault(file => string.Equals(file.FullPath, path, StringComparison.OrdinalIgnoreCase) || path.EndsWith(file.RelativePath, StringComparison.OrdinalIgnoreCase));
    }

    private static string MapRuleId(string engineRuleId, string language)
    {
        if (engineRuleId.Contains("use-after", StringComparison.OrdinalIgnoreCase))
        {
            return language == "c" ? "Quectel-C-010" : "Quectel-CPP-012";
        }

        if (engineRuleId.Contains("uninitialized", StringComparison.OrdinalIgnoreCase) || engineRuleId.Contains("uninit", StringComparison.OrdinalIgnoreCase))
        {
            return language == "c" ? "Quectel-C-006" : "Quectel-CPP-015";
        }

        if (engineRuleId.Contains("virtual", StringComparison.OrdinalIgnoreCase) || engineRuleId.Contains("OOP52", StringComparison.OrdinalIgnoreCase))
        {
            return "Quectel-CPP-005";
        }

        if (engineRuleId.Contains("return-stack", StringComparison.OrdinalIgnoreCase) || engineRuleId.Contains("return", StringComparison.OrdinalIgnoreCase) && engineRuleId.Contains("local", StringComparison.OrdinalIgnoreCase))
        {
            return language == "c" ? "Quectel-CERT-C-020" : "Quectel-CPP-035";
        }

        return language == "c" ? "Quectel-CERT-C-003" : "Quectel-CERT-CPP-020";
    }
}
