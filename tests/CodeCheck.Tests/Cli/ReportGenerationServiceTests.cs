using CodeCheck.Cli.Services;
using CodeCheck.Core.Configuration;
using CodeCheck.Core.Reports;

namespace CodeCheck.Tests.Cli;

public sealed class ReportGenerationServiceTests
{
    [Fact]
    public async Task GenerateAsync_UsesConfiguredReportFormats()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), $"codecheck-reports-{Guid.NewGuid():N}");
        var report = new ScanReport
        {
            ReportId = "test-report",
            Project = new ProjectInfo { Name = "test-project" }
        };
        var config = new ReportConfig
        {
            OutputDirectory = outputDirectory,
            Formats = ["json", "csv"]
        };

        try
        {
            var result = await new ReportGenerationService().GenerateAsync(
                report: report,
                reportConfig: config,
                outputDirectory: outputDirectory,
                fileCount: 0,
                finishedAt: DateTime.UtcNow,
                cancellationToken: CancellationToken.None);

            Assert.True(File.Exists(result.JsonPath));
            Assert.True(File.Exists(result.CsvPath));
            Assert.False(File.Exists(Path.Combine(outputDirectory, "report.html")));
            Assert.False(File.Exists(Path.Combine(outputDirectory, "report.sarif")));
            Assert.True(File.Exists(result.LogPath));
            Assert.True(report.Outputs.ContainsKey("json"));
            Assert.True(report.Outputs.ContainsKey("csv"));
            Assert.True(report.Outputs.ContainsKey("log"));
            Assert.False(report.Outputs.ContainsKey("html"));
            Assert.False(report.Outputs.ContainsKey("sarif"));
            Assert.False(report.Outputs.ContainsKey("xlsx"));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }
}
