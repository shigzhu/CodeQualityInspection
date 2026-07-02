using System.Text.Json;
using CodeCheck.Core.Reports;
using CodeCheck.Reporting.Csv;
using CodeCheck.Reporting.Html;
using CodeCheck.Reporting.Sarif;

namespace CodeCheck.Cli.Services;

public sealed class ExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly CsvReportWriter _csvReportWriter = new();
    private readonly HtmlReportWriter _htmlReportWriter = new();
    private readonly SarifReportWriter _sarifReportWriter = new();

    public async Task<int> ExportCsvAsync(string reportPath, string? outputPath, CancellationToken cancellationToken)
    {
        var report = await LoadReportAsync(reportPath, cancellationToken);
        if (report is null) return 2;

        var csvPath = string.IsNullOrWhiteSpace(outputPath)
            ? Path.ChangeExtension(reportPath, ".csv")
            : outputPath;

        await _csvReportWriter.WriteAsync(report, csvPath, cancellationToken);
        Console.WriteLine($"CSV exported: {csvPath}");
        return 0;
    }

    public async Task<int> ExportHtmlAsync(string reportPath, string? outputPath, CancellationToken cancellationToken)
    {
        var report = await LoadReportAsync(reportPath, cancellationToken);
        if (report is null) return 2;

        var htmlPath = string.IsNullOrWhiteSpace(outputPath)
            ? Path.ChangeExtension(reportPath, ".html")
            : outputPath;

        await _htmlReportWriter.WriteAsync(report, htmlPath, cancellationToken);
        Console.WriteLine($"HTML exported: {htmlPath}");
        return 0;
    }

    public async Task<int> ExportSarifAsync(string reportPath, string? outputPath, CancellationToken cancellationToken)
    {
        var report = await LoadReportAsync(reportPath, cancellationToken);
        if (report is null) return 2;

        var sarifPath = string.IsNullOrWhiteSpace(outputPath)
            ? Path.ChangeExtension(reportPath, ".sarif")
            : outputPath;

        await _sarifReportWriter.WriteAsync(report, sarifPath, cancellationToken);
        Console.WriteLine($"SARIF exported: {sarifPath}");
        return 0;
    }

    private static async Task<ScanReport?> LoadReportAsync(string reportPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reportPath))
        {
            Console.Error.WriteLine("Missing --report option.");
            return null;
        }

        if (!File.Exists(reportPath))
        {
            Console.Error.WriteLine($"Report file not found: {reportPath}");
            return null;
        }

        await using var stream = File.OpenRead(reportPath);
        var report = await JsonSerializer.DeserializeAsync<ScanReport>(stream, JsonOptions, cancellationToken);
        if (report is null)
        {
            Console.Error.WriteLine($"Failed to parse report file: {reportPath}");
        }

        return report;
    }
}
