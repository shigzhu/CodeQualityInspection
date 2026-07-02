using System.Net;
using System.Text;
using CodeCheck.Core.Reports;

namespace CodeCheck.Reporting.Html;

public sealed class HtmlReportWriter
{
    public async Task WriteAsync(ScanReport report, string outputPath, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var html = new StringBuilder();
        html.AppendLine("<!doctype html>");
        html.AppendLine("<html lang=\"zh-CN\"><head><meta charset=\"utf-8\"><title>CodeCheck Report</title></head><body>");
        html.AppendLine("<h1>CodeCheck Report</h1>");
        html.AppendLine("<h2>Scan Summary</h2>");
        html.AppendLine($"<p>Project: {EncodeHtml(report.Project.Name)}</p>");
        html.AppendLine($"<p>Status: {EncodeHtml(report.Scan.Status)}</p>");
        html.AppendLine($"<p>Total Files: {report.Scan.TotalFilesScanned}</p>");
        html.AppendLine($"<p>Total Issues: {report.Summary.TotalIssues}</p>");
        html.AppendLine($"<p>Active Issues: {report.Summary.ActiveIssues}</p>");
        html.AppendLine($"<p>Suppressed Issues: {report.Summary.SuppressedIssueCount}</p>");
        html.AppendLine($"<p>Quality Score: {report.QualityScore.Score} ({EncodeHtml(report.QualityScore.Level)})</p>");

        html.AppendLine("<h2>Quality Score</h2>");
        html.AppendLine("<table border=\"1\"><tr><th>Severity</th><th>Count</th><th>Points Per Issue</th><th>Total Deduction</th></tr>");
        AppendQualityScoreRow(html, "Blocker", report.QualityScore.Deduction.Blocker.Count, report.QualityScore.Deduction.Blocker.PointsPerIssue, report.QualityScore.Deduction.Blocker.Total);
        AppendQualityScoreRow(html, "Critical", report.QualityScore.Deduction.Critical.Count, report.QualityScore.Deduction.Critical.PointsPerIssue, report.QualityScore.Deduction.Critical.Total);
        AppendQualityScoreRow(html, "Warning", report.QualityScore.Deduction.Warning.Count, report.QualityScore.Deduction.Warning.PointsPerIssue, report.QualityScore.Deduction.Warning.Total);
        AppendQualityScoreRow(html, "Suggestion", report.QualityScore.Deduction.Suggestion.Count, report.QualityScore.Deduction.Suggestion.PointsPerIssue, report.QualityScore.Deduction.Suggestion.Total);
        html.AppendLine("</table>");
        if (report.QualityScore.Warnings.Count > 0)
        {
            html.AppendLine("<ul>");
            foreach (var warning in report.QualityScore.Warnings)
            {
                html.AppendLine($"<li>{EncodeHtml(warning)}</li>");
            }
            html.AppendLine("</ul>");
        }

        html.AppendLine("<h2>Issue Distribution</h2>");
        AppendDictionaryTable(html, "By Severity", report.Summary.BySeverity);
        AppendDictionaryTable(html, "By Engine", report.Summary.ByEngine);
        AppendDictionaryTable(html, "By Language", report.Summary.ByLanguage);

        html.AppendLine("<h2>Baseline</h2>");
        html.AppendLine("<table border=\"1\"><tr><th>State</th><th>New</th><th>Existing</th><th>Fixed</th><th>Not Compared</th><th>Path</th></tr>");
        html.AppendLine($"<tr><td>{EncodeHtml(report.Baseline.State)}</td><td>{report.Summary.NewIssueCount}</td><td>{report.Summary.ExistingIssueCount}</td><td>{report.Summary.FixedIssueCount}</td><td>{report.Summary.NotComparedIssueCount}</td><td>{EncodeHtml(report.Baseline.BaselinePath)}</td></tr>");
        html.AppendLine("</table>");

        html.AppendLine("<h2>Suppression</h2>");
        html.AppendLine("<table border=\"1\"><tr><th>Enabled</th><th>Active Suppressions</th><th>Suppressed Issues</th><th>Path</th></tr>");
        html.AppendLine($"<tr><td>{report.Suppression.Enabled}</td><td>{report.Suppression.ActiveSuppressionCount}</td><td>{report.Suppression.SuppressedIssueCount}</td><td>{EncodeHtml(report.Suppression.SuppressionPath)}</td></tr>");
        html.AppendLine("</table>");

        if (report.SuppressedIssues.Count > 0)
        {
            html.AppendLine("<h3>Suppressed Issues</h3>");
            html.AppendLine("<table border=\"1\"><tr><th>Suppression ID</th><th>Scope</th><th>Rule</th><th>File</th><th>Line</th><th>Reason</th></tr>");
            foreach (var suppressedIssue in report.SuppressedIssues)
            {
                html.AppendLine($"<tr><td>{EncodeHtml(suppressedIssue.SuppressionId)}</td><td>{EncodeHtml(suppressedIssue.Scope)}</td><td>{EncodeHtml(suppressedIssue.RuleId)}</td><td>{EncodeHtml(suppressedIssue.File)}</td><td>{suppressedIssue.Line}</td><td>{EncodeHtml(suppressedIssue.Reason)}</td></tr>");
            }
            html.AppendLine("</table>");
        }

        html.AppendLine("<h2>Issues</h2>");
        html.AppendLine("<table border=\"1\"><tr><th>ID</th><th>Severity</th><th>Rule</th><th>Engine</th><th>Language</th><th>File</th><th>Line</th><th>Baseline</th><th>Suppression</th><th>Message</th></tr>");
        foreach (var issue in report.Issues)
        {
            html.AppendLine($"<tr><td>{EncodeHtml(issue.IssueId)}</td><td>{EncodeHtml(issue.Severity)}</td><td>{EncodeHtml(issue.RuleId)}</td><td>{EncodeHtml(issue.Engine)}</td><td>{EncodeHtml(issue.Language)}</td><td>{EncodeHtml(issue.File)}</td><td>{issue.Line}</td><td>{EncodeHtml(issue.BaselineState)}</td><td>{EncodeHtml(issue.SuppressionState)}</td><td>{EncodeHtml(issue.Message)}</td></tr>");
        }
        html.AppendLine("</table>");

        html.AppendLine("<h2>Failed Files</h2>");
        if (report.FailedFiles.Count == 0)
        {
            html.AppendLine("<p>No failed files.</p>");
        }
        else
        {
            html.AppendLine("<table border=\"1\"><tr><th>File</th><th>Stage</th><th>Error</th><th>Message</th></tr>");
            foreach (var failedFile in report.FailedFiles)
            {
                html.AppendLine($"<tr><td>{EncodeHtml(failedFile.RelativeFile)}</td><td>{EncodeHtml(failedFile.Stage)}</td><td>{EncodeHtml(failedFile.ErrorCode)}</td><td>{EncodeHtml(failedFile.Message)}</td></tr>");
            }
            html.AppendLine("</table>");
        }

        html.AppendLine("</body></html>");
        await File.WriteAllTextAsync(outputPath, html.ToString(), new UTF8Encoding(false), cancellationToken);
    }

    private static void AppendQualityScoreRow(StringBuilder html, string severity, int count, double pointsPerIssue, double total)
    {
        html.AppendLine($"<tr><td>{EncodeHtml(severity)}</td><td>{count}</td><td>{pointsPerIssue}</td><td>{total}</td></tr>");
    }

    private static void AppendDictionaryTable(StringBuilder html, string title, IReadOnlyDictionary<string, int> values)
    {
        html.AppendLine($"<h3>{EncodeHtml(title)}</h3>");
        if (values.Count == 0)
        {
            html.AppendLine("<p>No data.</p>");
            return;
        }

        html.AppendLine("<table border=\"1\"><tr><th>Name</th><th>Count</th></tr>");
        foreach (var item in values)
        {
            html.AppendLine($"<tr><td>{EncodeHtml(item.Key)}</td><td>{item.Value}</td></tr>");
        }
        html.AppendLine("</table>");
    }

    private static string EncodeHtml(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
