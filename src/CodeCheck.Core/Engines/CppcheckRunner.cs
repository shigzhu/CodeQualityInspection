using System.Xml.Linq;
using CodeCheck.Core.Build;
using CodeCheck.Core.Configuration;
using CodeCheck.Core.Inputs;
using CodeCheck.Core.Issues;
using CodeCheck.Core.Reports;
using CodeCheck.Core.Runtime;
using CodeCheck.Core.Rules;

namespace CodeCheck.Core.Engines;

public sealed class CppcheckRunner : IAnalysisEngine
{
    private readonly ExternalProcessRunner _processRunner;

    public CppcheckRunner()
        : this(new ExternalProcessRunner())
    {
    }

    public CppcheckRunner(ExternalProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    public string Name => "cppcheck";

    public async Task<EngineResult> AnalyzeAsync(CodeCheckConfig config, CompileContext compileContext, IReadOnlyList<ScanInputFile> files, CancellationToken cancellationToken)
    {
        var result = new EngineResult { EngineName = Name };
        if (!config.Engines.Cppcheck.Enabled)
        {
            return result;
        }

        if (string.IsNullOrWhiteSpace(config.Engines.Cppcheck.Path) || !File.Exists(config.Engines.Cppcheck.Path))
        {
            result.FailedFiles.Add(new FailedFile { Stage = "EngineRun", ErrorCode = "CppcheckMissing", Message = "cppcheck executable not found." });
            return result;
        }

        var targetFiles = files.Where(file => !file.IsHeader && File.Exists(file.FullPath)).ToList();
        if (targetFiles.Count == 0)
        {
            return result;
        }

        var arguments = BuildArguments(targetFiles, compileContext);

        var processResult = await _processRunner.RunAsync(config.Engines.Cppcheck.Path, arguments, Directory.GetCurrentDirectory(), TimeSpan.FromSeconds(120), cancellationToken);
        var xml = string.IsNullOrWhiteSpace(processResult.StandardError) ? processResult.StandardOutput : processResult.StandardError;
        result.Issues.AddRange(ParseIssues(xml, targetFiles));

        if (!processResult.Success && result.Issues.Count == 0)
        {
            result.FailedFiles.Add(new FailedFile { Stage = "EngineRun", ErrorCode = processResult.TimedOut ? "CppcheckTimeout" : "CppcheckFailed", Message = xml });
        }

        return result;
    }

    public static List<Issue> ParseIssues(string xml, IReadOnlyList<ScanInputFile> files)
    {
        return ParseIssues(xml, files, DefaultRuleMappingResolver.Value);
    }

    public static List<Issue> ParseIssues(string xml, IReadOnlyList<ScanInputFile> files, RuleMappingResolver mappingResolver)
    {
        var issues = new List<Issue>();
        if (string.IsNullOrWhiteSpace(xml) || !xml.Contains("<results", StringComparison.OrdinalIgnoreCase))
        {
            return issues;
        }

        var document = XDocument.Parse(xml);
        var issueIndex = 1;
        foreach (var error in document.Descendants("error"))
        {
            var location = error.Elements("location").FirstOrDefault();
            var filePath = location?.Attribute("file")?.Value ?? string.Empty;
            var line = int.TryParse(location?.Attribute("line")?.Value, out var parsedLine) ? parsedLine : 1;
            var file = ResolveFile(filePath, files);
            var severity = MapSeverity(error.Attribute("severity")?.Value ?? string.Empty);
            var engineRuleId = error.Attribute("id")?.Value ?? string.Empty;
            var message = error.Attribute("msg")?.Value ?? error.Attribute("verbose")?.Value ?? string.Empty;
            var language = file?.Language ?? string.Empty;
            var mapping = mappingResolver.Resolve("cppcheck", engineRuleId, language);

            issues.Add(new Issue
            {
                IssueId = $"CPPCHECK-{issueIndex:D6}",
                RuleId = mapping.RuleId,
                EngineRuleId = mapping.EngineRuleId,
                Severity = severity,
                Language = language,
                Engine = "cppcheck",
                Message = message,
                File = file?.RelativePath ?? filePath.Replace('/', Path.DirectorySeparatorChar),
                Line = line
            });
            issueIndex++;
        }

        return issues;
    }

    public static List<string> BuildArguments(IReadOnlyList<ScanInputFile> targetFiles, CompileContext compileContext)
    {
        var arguments = new List<string>
        {
            "--enable=warning,style,performance,portability",
            "--xml",
            "--xml-version=2"
        };
        arguments.AddRange(compileContext.IncludeDirectories.Select(include => $"-I{include}"));
        arguments.AddRange(compileContext.Defines.Select(define => $"-D{define}"));
        arguments.AddRange(targetFiles.Select(file => file.FullPath));
        return arguments;
    }

    private static ScanInputFile? ResolveFile(string path, IReadOnlyList<ScanInputFile> files)
    {
        return files.FirstOrDefault(file => string.Equals(file.FullPath, path, StringComparison.OrdinalIgnoreCase) || path.EndsWith(file.RelativePath, StringComparison.OrdinalIgnoreCase));
    }

    private static string MapSeverity(string severity)
    {
        return severity switch
        {
            "error" => "Critical",
            "warning" => "Warning",
            "performance" => "Suggestion",
            "style" => "Suggestion",
            "portability" => "Warning",
            _ => "Warning"
        };
    }

    private static readonly Lazy<RuleMappingResolver> DefaultRuleMappingResolver = new(() => RuleMappingResolverFactory.CreateDefault());
}
