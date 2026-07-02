using System.Text.Json;
using CodeCheck.Core.Issues;
using CodeCheck.Core.Reports;
using CodeCheck.Reporting.Sarif;

namespace CodeCheck.Tests.Reporting;

public sealed class SarifReportWriterTests
{
    [Fact]
    public async Task WriteAsync_CreatesValidSarifStructure()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"codecheck-{Guid.NewGuid():N}.sarif");
        var report = new ScanReport
        {
            Issues =
            [
                new Issue
                {
                    IssueId = "ISSUE-000001",
                    Fingerprint = "sha256-stable-test",
                    PrimaryFingerprint = "sha256-primary-test",
                    RuleId = "Quectel-CPP-001",
                    EngineRuleId = "cppcoreguidelines-avoid-non-const-global-variables",
                    Severity = "Warning",
                    Language = "cpp",
                    Engine = "CodeCheckBuiltin",
                    Message = "Header should not use using namespace.",
                    File = "include/bad_header.hpp",
                    Line = 6,
                    BaselineState = "Existing",
                    SuppressionState = "Active",
                    IsSuppressed = false
                }
            ]
        };

        try
        {
            await new SarifReportWriter().WriteAsync(report, outputPath, CancellationToken.None);

            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath));
            var root = document.RootElement;
            Assert.Equal("2.1.0", root.GetProperty("version").GetString());
            var run = root.GetProperty("runs")[0];
            var driver = run.GetProperty("tool").GetProperty("driver");
            Assert.Equal("CodeCheck", driver.GetProperty("name").GetString());
            var rule = driver.GetProperty("rules")[0];
            Assert.Equal("Quectel-CPP-001", rule.GetProperty("id").GetString());
            Assert.Equal("CodeCheckBuiltin", rule.GetProperty("properties").GetProperty("engine").GetString());
            var result = run.GetProperty("results")[0];
            Assert.Equal("Quectel-CPP-001", result.GetProperty("ruleId").GetString());
            Assert.Equal("warning", result.GetProperty("level").GetString());
            Assert.Equal("sha256-stable-test", result.GetProperty("fingerprints").GetProperty("stable").GetString());
            Assert.Equal("Existing", result.GetProperty("properties").GetProperty("baselineState").GetString());
            Assert.Equal("cppcoreguidelines-avoid-non-const-global-variables", result.GetProperty("properties").GetProperty("engineRuleId").GetString());
            Assert.False(result.GetProperty("properties").GetProperty("isSuppressed").GetBoolean());
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
