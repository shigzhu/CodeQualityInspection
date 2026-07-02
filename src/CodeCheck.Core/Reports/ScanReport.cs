using CodeCheck.Core.Issues;
using CodeCheck.Core.Baseline;
using CodeCheck.Core.Suppression;
using CodeCheck.Core.Quality;

namespace CodeCheck.Core.Reports;

public sealed class ScanReport
{
    public string SchemaVersion { get; set; } = "1.0.0";
    public string ReportId { get; set; } = string.Empty;
    public ToolInfo Tool { get; set; } = new();
    public ProjectInfo Project { get; set; } = new();
    public ScanInfo Scan { get; set; } = new();
    public SummaryInfo Summary { get; set; } = new();
    public QualityScoreInfo QualityScore { get; set; } = new();
    public ReportMetrics Metrics { get; set; } = new();
    public List<Issue> Issues { get; set; } = [];
    public List<FailedFile> FailedFiles { get; set; } = [];
    public List<DisabledRuleInfo> DisabledRules { get; set; } = [];
    public BaselineInfo Baseline { get; set; } = new();
    public SuppressionInfo Suppression { get; set; } = new();
    public List<SuppressedIssue> SuppressedIssues { get; set; } = [];
    public Dictionary<string, string> Outputs { get; set; } = [];
    public List<LogEntry> Logs { get; set; } = [];
}

public sealed class ToolInfo
{
    public string Name { get; set; } = "CodeCheck";
    public string CliVersion { get; set; } = "1.0.0";
}

public sealed class ProjectInfo
{
    public string Name { get; set; } = string.Empty;
    public string Root { get; set; } = string.Empty;
    public string ProjectKey { get; set; } = string.Empty;
}

public sealed class ScanInfo
{
    public DateTime StartedAt { get; set; }
    public DateTime FinishedAt { get; set; }
    public string Status { get; set; } = "NotStarted";
    public string InputType { get; set; } = string.Empty;
    public int TotalFilesDiscovered { get; set; }
    public int TotalFilesScheduled { get; set; }
    public int TotalFilesScanned { get; set; }
    public int TotalFilesFailed { get; set; }
}

public sealed class SummaryInfo
{
    public int TotalIssues { get; set; }
    public int ActiveIssues { get; set; }
    public int SuppressedIssueCount { get; set; }
    public int NewIssueCount { get; set; }
    public int ExistingIssueCount { get; set; }
    public int FixedIssueCount { get; set; }
    public int NotComparedIssueCount { get; set; }
    public Dictionary<string, int> BySeverity { get; set; } = [];
    public Dictionary<string, int> ByLanguage { get; set; } = [];
    public Dictionary<string, int> ByEngine { get; set; } = [];
}

public sealed class FailedFile
{
    public string File { get; set; } = string.Empty;
    public string RelativeFile { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class ReportMetrics
{
    public Dictionary<string, int> FilesByLanguage { get; set; } = [];
    public Dictionary<string, int> LinesOfCodeByLanguage { get; set; } = [];
    public List<ComplexityMetric> TopComplexityFunctions { get; set; } = [];
    public List<FunctionLengthMetric> TopLongFunctions { get; set; } = [];
}

public sealed class ComplexityMetric
{
    public string File { get; set; } = string.Empty;
    public string Function { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Complexity { get; set; }
}

public sealed class FunctionLengthMetric
{
    public string File { get; set; } = string.Empty;
    public string Function { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Length { get; set; }
}

public sealed class DisabledRuleInfo
{
    public string RuleId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string DisabledBy { get; set; } = string.Empty;
    public DateTime DisabledAt { get; set; } = DateTime.Now;
    public string RiskConfirmation { get; set; } = string.Empty;
}

public sealed class LogEntry
{
    public string Level { get; set; } = "Info";
    public DateTime Time { get; set; } = DateTime.Now;
    public string Message { get; set; } = string.Empty;
}
