using CodeCheck.Core.Reports;
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

    public async Task<ReportGenerationResult> GenerateAsync(ScanReport report, string outputDirectory, int fileCount, DateTime finishedAt, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputDirectory);

        var result = new ReportGenerationResult(
            JsonPath: Path.Combine(outputDirectory, "report.json"),
            HtmlPath: Path.Combine(outputDirectory, "report.html"),
            SarifPath: Path.Combine(outputDirectory, "report.sarif"),
            CsvPath: Path.Combine(outputDirectory, "report.csv"),
            LogPath: Path.Combine(outputDirectory, "scan.log"));

        report.Outputs = new Dictionary<string, string>
        {
            ["json"] = result.JsonPath,
            ["html"] = result.HtmlPath,
            ["sarif"] = result.SarifPath,
            ["csv"] = result.CsvPath,
            ["log"] = result.LogPath
        };

        await _reportJsonWriter.WriteAsync(report, result.JsonPath, cancellationToken);
        await _htmlReportWriter.WriteAsync(report, result.HtmlPath, cancellationToken);
        await _sarifReportWriter.WriteAsync(report, result.SarifPath, cancellationToken);
        await _csvReportWriter.WriteAsync(report, result.CsvPath, cancellationToken);
        await File.WriteAllTextAsync(result.LogPath, $"[{finishedAt:O}] Scan completed. Files: {fileCount}{Environment.NewLine}", cancellationToken);

        return result;
    }
}

public sealed record ReportGenerationResult(string JsonPath, string HtmlPath, string SarifPath, string CsvPath, string LogPath);
