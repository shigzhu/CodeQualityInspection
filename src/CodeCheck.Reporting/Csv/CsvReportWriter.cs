using System.Text;
using CodeCheck.Core.Reports;

namespace CodeCheck.Reporting.Csv;

public sealed class CsvReportWriter
{
    public async Task WriteAsync(ScanReport report, string outputPath, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var csv = new StringBuilder();
        AppendRow(csv,
        [
            "IssueId",
            "Severity",
            "RuleId",
            "Engine",
            "Language",
            "File",
            "Line",
            "BaselineState",
            "SuppressionState",
            "IsSuppressed",
            "Fingerprint",
            "Message"
        ]);

        foreach (var issue in report.Issues)
        {
            AppendRow(csv,
            [
                issue.IssueId,
                issue.Severity,
                issue.RuleId,
                issue.Engine,
                issue.Language,
                issue.File,
                issue.Line.ToString(),
                issue.BaselineState,
                issue.SuppressionState,
                issue.IsSuppressed ? "true" : "false",
                issue.Fingerprint,
                issue.Message
            ]);
        }

        await File.WriteAllTextAsync(outputPath, csv.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), cancellationToken);
    }

    private static void AppendRow(StringBuilder csv, IReadOnlyList<string> values)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (i > 0)
            {
                csv.Append(',');
            }

            csv.Append(Escape(values[i]));
        }

        csv.AppendLine();
    }

    private static string Escape(string value)
    {
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\r') && !value.Contains('\n'))
        {
            return value;
        }

        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
