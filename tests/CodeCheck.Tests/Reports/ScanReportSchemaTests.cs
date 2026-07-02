using System.Text.Json;
using CodeCheck.Core.Reports;
using CodeCheck.Reporting.Json;

namespace CodeCheck.Tests.Reports;

public sealed class ScanReportSchemaTests
{
    [Fact]
    public async Task JsonReport_IncludesV1SchemaSections()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"codecheck-schema-{Guid.NewGuid():N}.json");
        var report = new ScanReport
        {
            Metrics = new ReportMetrics
            {
                FilesByLanguage = { ["cpp"] = 2 },
                LinesOfCodeByLanguage = { ["cpp"] = 120 }
            },
            DisabledRules =
            [
                new DisabledRuleInfo
                {
                    RuleId = "Quectel-CPP-001",
                    Reason = "Accepted project-specific exception.",
                    DisabledBy = "tester"
                }
            ],
            Outputs = { ["json"] = outputPath }
        };

        try
        {
            await new ReportJsonWriter().WriteAsync(report, outputPath, CancellationToken.None);

            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath));
            var root = document.RootElement;

            Assert.True(root.TryGetProperty("metrics", out var metrics));
            Assert.Equal(2, metrics.GetProperty("filesByLanguage").GetProperty("cpp").GetInt32());
            Assert.Equal(120, metrics.GetProperty("linesOfCodeByLanguage").GetProperty("cpp").GetInt32());

            Assert.True(root.TryGetProperty("disabledRules", out var disabledRules));
            Assert.Equal("Quectel-CPP-001", disabledRules[0].GetProperty("ruleId").GetString());
            Assert.Equal("Accepted project-specific exception.", disabledRules[0].GetProperty("reason").GetString());

            Assert.True(root.TryGetProperty("outputs", out var outputs));
            Assert.True(outputs.TryGetProperty("json", out _));
            Assert.False(outputs.TryGetProperty("xlsx", out _));
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }
}
