using CodeCheck.Core.Issues;
using CodeCheck.Core.Quality;
using CodeCheck.Core.Reports;

namespace CodeCheck.Tests.Quality;

public sealed class QualityScoreServiceTests
{
    [Fact]
    public void Calculate_DeductsPointsByActiveIssueSeverity()
    {
        var issues = new List<Issue>
        {
            new() { Severity = "Blocker" },
            new() { Severity = "Critical" },
            new() { Severity = "Warning" },
            new() { Severity = "Suggestion" }
        };

        var score = new QualityScoreService().Calculate(issues, []);

        Assert.Equal(82.5, score.Score);
        Assert.Equal("良好", score.Level);
        Assert.Equal(10, score.Deduction.Blocker.Total);
        Assert.Equal(5, score.Deduction.Critical.Total);
        Assert.Equal(2, score.Deduction.Warning.Total);
        Assert.Equal(0.5, score.Deduction.Suggestion.Total);
    }

    [Fact]
    public void Calculate_ExcludesSuppressedIssues()
    {
        var issues = new List<Issue>
        {
            new() { Severity = "Blocker", IsSuppressed = true },
            new() { Severity = "Warning" }
        };

        var score = new QualityScoreService().Calculate(issues, []);

        Assert.Equal(98, score.Score);
        Assert.Equal(1, score.ExcludedSuppressedIssueCount);
        Assert.Contains(score.Warnings, warning => warning.Contains("已抑制", StringComparison.Ordinal));
    }

    [Fact]
    public void Calculate_DoesNotGoBelowZeroAndWarnsForFailedFiles()
    {
        var issues = Enumerable.Range(0, 20).Select(_ => new Issue { Severity = "Blocker" }).ToList();

        var score = new QualityScoreService().Calculate(issues, [new FailedFile { File = "src/main.cpp" }]);

        Assert.Equal(0, score.Score);
        Assert.Equal("高风险", score.Level);
        Assert.Contains(score.Warnings, warning => warning.Contains("扫描失败文件", StringComparison.Ordinal));
    }
}
