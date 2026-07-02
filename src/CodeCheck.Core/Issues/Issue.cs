namespace CodeCheck.Core.Issues;

public sealed class Issue
{
    public string IssueId { get; set; } = string.Empty;
    public string Fingerprint { get; set; } = string.Empty;
    public string PrimaryFingerprint { get; set; } = string.Empty;
    public string RuleId { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Engine { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string File { get; set; } = string.Empty;
    public int Line { get; set; }
    public string BaselineState { get; set; } = string.Empty;
    public string SuppressionState { get; set; } = string.Empty;
    public bool IsSuppressed { get; set; }
}
