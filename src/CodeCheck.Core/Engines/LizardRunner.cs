using System.Globalization;
using System.Text.RegularExpressions;
using CodeCheck.Core.Build;
using CodeCheck.Core.Configuration;
using CodeCheck.Core.Inputs;
using CodeCheck.Core.Issues;
using CodeCheck.Core.Reports;
using CodeCheck.Core.Runtime;

namespace CodeCheck.Core.Engines;

public sealed class LizardRunner : IAnalysisEngine
{
    private static readonly Regex RowPattern = new(@"^\s*(?<nloc>\d+)\s+(?<ccn>\d+)\s+(?<token>\d+)\s+(?<params>\d+)\s+(?<length>\d+)\s+(?<location>.+)$", RegexOptions.Compiled);
    private readonly ExternalProcessRunner _processRunner = new();

    public string Name => "lizard";

    public async Task<EngineResult> AnalyzeAsync(CodeCheckConfig config, CompileContext compileContext, IReadOnlyList<ScanInputFile> files, CancellationToken cancellationToken)
    {
        var result = new EngineResult { EngineName = Name };
        if (!config.Engines.Lizard.Enabled)
        {
            return result;
        }

        if (string.IsNullOrWhiteSpace(config.Engines.Lizard.Path) || !File.Exists(config.Engines.Lizard.Path))
        {
            result.FailedFiles.Add(new FailedFile { Stage = "EngineRun", ErrorCode = "LizardMissing", Message = "lizard executable not found." });
            return result;
        }

        var targetFiles = files.Where(file => !file.IsHeader && File.Exists(file.FullPath)).ToList();
        if (targetFiles.Count == 0)
        {
            return result;
        }

        var processResult = await _processRunner.RunAsync(config.Engines.Lizard.Path, targetFiles.Select(file => file.FullPath), Directory.GetCurrentDirectory(), TimeSpan.FromSeconds(300), cancellationToken);
        if (!processResult.Success)
        {
            result.FailedFiles.Add(new FailedFile { Stage = "EngineRun", ErrorCode = processResult.TimedOut ? "LizardTimeout" : "LizardFailed", Message = processResult.StandardError });
            return result;
        }

        result.Issues.AddRange(ParseIssues(processResult.StandardOutput, targetFiles));
        return result;
    }

    public static List<Issue> ParseIssues(string output, IReadOnlyList<ScanInputFile> files, int complexityThreshold = 10, int functionLinesThreshold = 100)
    {
        var issues = new List<Issue>();
        var issueIndex = 1;
        foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var match = RowPattern.Match(line);
            if (!match.Success) continue;

            var ccn = int.Parse(match.Groups["ccn"].Value, CultureInfo.InvariantCulture);
            var length = int.Parse(match.Groups["length"].Value, CultureInfo.InvariantCulture);
            var location = match.Groups["location"].Value;
            var file = files.FirstOrDefault(x => location.Contains(x.FullPath, StringComparison.OrdinalIgnoreCase) || location.Contains(x.RelativePath, StringComparison.OrdinalIgnoreCase));
            var language = file?.Language == "c" ? "c" : "cpp";
            var lineNumber = ResolveLine(location);

            if (ccn > complexityThreshold)
            {
                issues.Add(new Issue { IssueId = $"LIZARD-{issueIndex:D6}", RuleId = language == "c" ? "Quectel-C-021" : "Quectel-CPP-037", Severity = "Warning", Language = language, Engine = "lizard", Message = $"Function cyclomatic complexity is {ccn}, greater than {complexityThreshold}.", File = file?.RelativePath ?? location, Line = lineNumber });
                issueIndex++;
            }

            if (length > functionLinesThreshold)
            {
                issues.Add(new Issue { IssueId = $"LIZARD-{issueIndex:D6}", RuleId = language == "c" ? "Quectel-C-022" : "Quectel-CPP-038", Severity = "Warning", Language = language, Engine = "lizard", Message = $"Function length is {length}, greater than {functionLinesThreshold}.", File = file?.RelativePath ?? location, Line = lineNumber });
                issueIndex++;
            }
        }

        return issues;
    }

    private static int ResolveLine(string location)
    {
        foreach (var part in location.Split(':' ).Reverse())
        {
            if (int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)) return value;
        }

        return 1;
    }
}
