using CodeCheck.Core.Baseline;
using CodeCheck.Core.Issues;
using CodeCheck.Core.Suppression;

namespace CodeCheck.Core.Reports;

public sealed class SummaryService
{
    public SummaryInfo Build(IReadOnlyList<Issue> issues, BaselineInfo baselineInfo, SuppressionInfo suppressionInfo)
    {
        var activeIssues = issues.Where(issue => !issue.IsSuppressed).ToList();
        return new SummaryInfo
        {
            TotalIssues = issues.Count,
            ActiveIssues = activeIssues.Count,
            SuppressedIssueCount = suppressionInfo.SuppressedIssueCount,
            NewIssueCount = baselineInfo.Summary.NewIssues,
            ExistingIssueCount = baselineInfo.Summary.ExistingIssues,
            FixedIssueCount = baselineInfo.Summary.FixedIssues,
            NotComparedIssueCount = baselineInfo.Summary.NotComparedIssues,
            BySeverity = CountBy(activeIssues, issue => issue.Severity),
            ByLanguage = CountBy(activeIssues, issue => string.IsNullOrWhiteSpace(issue.Language) ? "unknown" : issue.Language),
            ByEngine = CountBy(activeIssues, issue => string.IsNullOrWhiteSpace(issue.Engine) ? "unknown" : issue.Engine)
        };
    }

    private static Dictionary<string, int> CountBy(IEnumerable<Issue> issues, Func<Issue, string> selector)
    {
        return issues
            .GroupBy(selector, StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
    }
}
