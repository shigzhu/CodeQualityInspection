using CodeCheck.Core.Reports;
using CodeCheck.Core.Configuration;
using CodeCheck.Reporting.Csv;
using CodeCheck.Reporting.Html;
using CodeCheck.Reporting.Json;
using CodeCheck.Reporting.Sarif;

namespace CodeCheck.Cli.Services;

public sealed class ReportGenerationService
{
    private readonly ReportJsonWriter _reportJsonWriter = new();
    private readonly HtmlReportWriter _htmlReportWriter = new();
    private readonly SarifReportWriter _sarifReportWriter = new();
    private readonly CsvReportWriter _csvReportWriter = new();

    public async Task<ReportGenerationResult> GenerateAsync(ScanReport report, ReportConfig reportConfig, string outputDirectory, int fileCount, DateTime finishedAt, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputDirectory);
        var formats = reportConfig.Formats.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var result = new ReportGenerationResult(
            JsonPath: Path.Combine(outputDirectory, "report.json"),
            HtmlPath: Path.Combine(outputDirectory, "report.html"),
            SarifPath: Path.Combine(outputDirectory, "report.sarif"),
            CsvPath: Path.Combine(outputDirectory, "report.csv"),
            LogPath: Path.Combine(outputDirectory, "scan.log"));

        report.Outputs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["log"] = result.LogPath
        };

        if (formats.Contains("json"))
        {
            report.Outputs["json"] = result.JsonPath;
        }

        if (formats.Contains("html"))
        {
            report.Outputs["html"] = result.HtmlPath;
        }

        if (formats.Contains("sarif"))
        {
            report.Outputs["sarif"] = result.SarifPath;
        }

        if (formats.Contains("csv"))
        {
            report.Outputs["csv"] = result.CsvPath;
        }

        if (formats.Contains("json"))
        {
            await _reportJsonWriter.WriteAsync(report, result.JsonPath, cancellationToken);
        }

        if (formats.Contains("html"))
        {
            await _htmlReportWriter.WriteAsync(report, result.HtmlPath, cancellationToken);
        }

        if (formats.Contains("sarif"))
        {
            await _sarifReportWriter.WriteAsync(report, result.SarifPath, cancellationToken);
        }

        if (formats.Contains("csv"))
        {
            await _csvReportWriter.WriteAsync(report, result.CsvPath, cancellationToken);
        }

        await File.WriteAllTextAsync(result.LogPath, $"[{finishedAt:O}] Scan completed. Files: {fileCount}{Environment.NewLine}", cancellationToken);

        return result;
    }
}

public sealed record ReportGenerationResult(string JsonPath, string HtmlPath, string SarifPath, string CsvPath, string LogPath);
