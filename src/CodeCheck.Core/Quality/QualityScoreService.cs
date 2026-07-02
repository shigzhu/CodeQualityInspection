using CodeCheck.Core.Issues;
using CodeCheck.Core.Reports;

namespace CodeCheck.Core.Quality;

public interface IQualityScoreService
{
    QualityScoreInfo Calculate(IReadOnlyList<Issue> issues, IReadOnlyList<FailedFile> failedFiles);
}

public sealed class QualityScoreService : IQualityScoreService
{
    public QualityScoreInfo Calculate(IReadOnlyList<Issue> issues, IReadOnlyList<FailedFile> failedFiles)
    {
        var activeIssues = issues.Where(issue => !issue.IsSuppressed).ToList();
        var deduction = new QualityScoreDeduction
        {
            Blocker = CreateDeduction(activeIssues, "Blocker", 10),
            Critical = CreateDeduction(activeIssues, "Critical", 5),
            Warning = CreateDeduction(activeIssues, "Warning", 2),
            Suggestion = CreateDeduction(activeIssues, "Suggestion", 0.5)
        };

        var totalDeduction = deduction.Blocker.Total + deduction.Critical.Total + deduction.Warning.Total + deduction.Suggestion.Total;
        var score = Math.Max(0, 100 - totalDeduction);
        var result = new QualityScoreInfo
        {
            Enabled = true,
            BaseScore = 100,
            Score = score,
            Level = ResolveLevel(score),
            MinimumScore = 0,
            Deduction = deduction,
            ExcludedSuppressedIssueCount = issues.Count(issue => issue.IsSuppressed)
        };

        if (failedFiles.Count > 0)
        {
            result.Warnings.Add("存在扫描失败文件，质量评分可能不完整。");
        }

        if (result.ExcludedSuppressedIssueCount > 0)
        {
            result.Warnings.Add("存在已抑制问题，评分未包含这些问题。");
        }

        return result;
    }

    private static QualityScoreDeductionItem CreateDeduction(IReadOnlyList<Issue> issues, string severity, double pointsPerIssue)
    {
        var count = issues.Count(issue => string.Equals(issue.Severity, severity, StringComparison.OrdinalIgnoreCase));
        return new QualityScoreDeductionItem
        {
            Count = count,
            PointsPerIssue = pointsPerIssue,
            Total = count * pointsPerIssue
        };
    }

    private static string ResolveLevel(double score)
    {
        return score switch
        {
            >= 90 => "优秀",
            >= 80 => "良好",
            >= 70 => "一般",
            >= 60 => "较差",
            _ => "高风险"
        };
    }
}

public sealed class QualityScoreInfo
{
    public bool Enabled { get; set; }
    public double BaseScore { get; set; } = 100;
    public double Score { get; set; }
    public string Level { get; set; } = string.Empty;
    public QualityScoreDeduction Deduction { get; set; } = new();
    public double MinimumScore { get; set; }
    public int ExcludedSuppressedIssueCount { get; set; }
    public List<string> Warnings { get; set; } = [];
}

public sealed class QualityScoreDeduction
{
    public QualityScoreDeductionItem Blocker { get; set; } = new();
    public QualityScoreDeductionItem Critical { get; set; } = new();
    public QualityScoreDeductionItem Warning { get; set; } = new();
    public QualityScoreDeductionItem Suggestion { get; set; } = new();
}

public sealed class QualityScoreDeductionItem
{
    public int Count { get; set; }
    public double PointsPerIssue { get; set; }
    public double Total { get; set; }
}

