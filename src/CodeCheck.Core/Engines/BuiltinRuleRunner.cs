using System.Text.RegularExpressions;
using CodeCheck.Core.Configuration;
using CodeCheck.Core.Build;
using CodeCheck.Core.Inputs;
using CodeCheck.Core.Issues;

namespace CodeCheck.Core.Engines;

public sealed class BuiltinRuleRunner : IAnalysisEngine
{
    public string Name => "CodeCheckBuiltin";

    private static readonly IReadOnlyList<ForbiddenApiRule> ForbiddenApiRules =
    [
        new("Quectel-C-001", "Blocker", "\u7981\u6b62\u4f7f\u7528 gets \u7b49\u65e0\u8fb9\u754c\u8f93\u5165\u51fd\u6570", @"\bgets\s*\("),
        new("Quectel-C-002", "Critical", "\u7981\u6b62\u4f7f\u7528\u4e0d\u9650\u5236\u957f\u5ea6\u7684\u5b57\u7b26\u4e32\u62f7\u8d1d\u51fd\u6570", @"\bstrcpy\s*\("),
        new("Quectel-C-003", "Critical", "\u7981\u6b62\u4f7f\u7528\u4e0d\u9650\u5236\u957f\u5ea6\u7684\u683c\u5f0f\u5316\u8f93\u51fa\u51fd\u6570", @"\bsprintf\s*\(")
    ];

    private static readonly Regex HeaderUsingNamespacePattern = new(@"\busing\s+namespace\s+\w+\s*;", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex HeaderNonInlineFunctionPattern = new(@"^\s*(?!inline\b)(?:[A-Za-z_]\w*|[A-Za-z_:][\w:<>]*)(?:[\s\*&]+[A-Za-z_:][\w:<>]*)*\s+[A-Za-z_]\w*\s*\([^;]*\)\s*\{", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex HeaderFunctionSignaturePattern = new(@"^\s*(?!inline\b)(?:[A-Za-z_]\w*|[A-Za-z_:][\w:<>]*)(?:[\s\*&]+[A-Za-z_:][\w:<>]*)*\s+[A-Za-z_]\w*\s*\([^;]*\)\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public async Task<EngineResult> AnalyzeAsync(CodeCheckConfig config, CompileContext compileContext, IReadOnlyList<ScanInputFile> files, CancellationToken cancellationToken)
    {
        var issues = new List<Issue>();
        var issueIndex = 1;

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(file.FullPath))
            {
                continue;
            }

            var lines = await File.ReadAllLinesAsync(file.FullPath, cancellationToken);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                foreach (var rule in ForbiddenApiRules)
                {
                    if (!rule.Pattern.IsMatch(line))
                    {
                        continue;
                    }

                    issues.Add(new Issue
                    {
                        IssueId = $"ISSUE-{issueIndex:D6}",
                        RuleId = rule.RuleId,
                        EngineRuleId = rule.RuleId,
                        Severity = rule.Severity,
                        Language = file.Language,
                        Engine = Name,
                        Message = rule.Message,
                        File = file.RelativePath,
                        Line = i + 1
                    });
                    issueIndex++;
                }

                if (!file.IsHeader)
                {
                    continue;
                }

                if (HeaderUsingNamespacePattern.IsMatch(line))
                {
                    issues.Add(CreateIssue(issueIndex++, "Quectel-CPP-001", "Warning", "\u5934\u6587\u4ef6\u4e2d\u7981\u6b62\u4f7f\u7528 using namespace", file, i + 1));
                }

                if (HeaderNonInlineFunctionPattern.IsMatch(line) || IsMultiLineHeaderFunctionDefinition(lines, i))
                {
                    issues.Add(CreateIssue(issueIndex++, "Quectel-CPP-002", "Warning", "\u5934\u6587\u4ef6\u4e2d\u7981\u6b62\u5b9a\u4e49\u975e inline \u666e\u901a\u51fd\u6570", file, i + 1));
                }
            }
        }

        return new EngineResult
        {
            EngineName = Name,
            Issues = issues
        };
    }

    public async Task<IReadOnlyList<Issue>> AnalyzeAsync(IReadOnlyList<ScanInputFile> files, CancellationToken cancellationToken)
    {
        var result = await AnalyzeAsync(new CodeCheckConfig(), new CompileContext(), files, cancellationToken);
        return result.Issues;
    }

    private static Issue CreateIssue(int issueIndex, string ruleId, string severity, string message, ScanInputFile file, int line)
    {
        return new Issue
        {
            IssueId = $"ISSUE-{issueIndex:D6}",
            RuleId = ruleId,
            EngineRuleId = ruleId,
            Severity = severity,
            Language = file.Language,
            Engine = "CodeCheckBuiltin",
            Message = message,
            File = file.RelativePath,
            Line = line
        };
    }

    private static bool IsMultiLineHeaderFunctionDefinition(string[] lines, int index)
    {
        if (!HeaderFunctionSignaturePattern.IsMatch(lines[index]))
        {
            return false;
        }

        for (var i = index + 1; i < lines.Length; i++)
        {
            var nextLine = lines[i].Trim();
            if (nextLine.Length == 0)
            {
                continue;
            }

            return nextLine.StartsWith("{", StringComparison.Ordinal);
        }

        return false;
    }

    private sealed class ForbiddenApiRule
    {
        public ForbiddenApiRule(string ruleId, string severity, string message, string pattern)
        {
            RuleId = ruleId;
            Severity = severity;
            Message = message;
            Pattern = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);
        }

        public string RuleId { get; }
        public string Severity { get; }
        public string Message { get; }
        public Regex Pattern { get; }
    }
}
