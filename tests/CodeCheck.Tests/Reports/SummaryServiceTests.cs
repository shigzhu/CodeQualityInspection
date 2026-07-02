using CodeCheck.Core.Baseline;
using CodeCheck.Core.Issues;
using CodeCheck.Core.Reports;
using CodeCheck.Core.Suppression;

namespace CodeCheck.Tests.Reports;

public sealed class SummaryServiceTests
{
    [Fact]
    public void Build_CreatesExtendedSummaryFromActiveIssues()
    {
        var issues = new List<Issue>
        {
            new() { Severity = "Warning", Language = "cpp", Engine = "CodeCheckBuiltin" },
            new() { Severity = "Critical", Language = "c", Engine = "cppcheck" },
            new() { Severity = "Warning", Language = "cpp", Engine = "clang-tidy", IsSuppressed = true }
        };
        var baselineInfo = new BaselineInfo
        {
            Summary = new BaselineCompareSummary
            {
                NewIssues = 1,
                ExistingIssues = 1,
                FixedIssues = 2,
                NotComparedIssues = 0
            }
        };
        var suppressionInfo = new SuppressionInfo { SuppressedIssueCount = 1 };

        var summary = new SummaryService().Build(issues, baselineInfo, suppressionInfo);

        Assert.Equal(3, summary.TotalIssues);
        Assert.Equal(2, summary.ActiveIssues);
        Assert.Equal(1, summary.SuppressedIssueCount);
        Assert.Equal(1, summary.NewIssueCount);
        Assert.Equal(1, summary.ExistingIssueCount);
        Assert.Equal(2, summary.FixedIssueCount);
        Assert.Equal(1, summary.BySeverity["Warning"]);
        Assert.Equal(1, summary.BySeverity["Critical"]);
        Assert.Equal(1, summary.ByLanguage["cpp"]);
        Assert.Equal(1, summary.ByLanguage["c"]);
        Assert.Equal(1, summary.ByEngine["CodeCheckBuiltin"]);
        Assert.Equal(1, summary.ByEngine["cppcheck"]);
        Assert.False(summary.ByEngine.ContainsKey("clang-tidy"));
    }
}
