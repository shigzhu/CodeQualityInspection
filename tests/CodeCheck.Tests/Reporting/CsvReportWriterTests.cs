using CodeCheck.Core.Issues;
using CodeCheck.Core.Reports;
using CodeCheck.Reporting.Csv;

namespace CodeCheck.Tests.Reporting;

public sealed class CsvReportWriterTests
{
    [Fact]
    public async Task WriteAsync_CreatesIssueCsvWithEscapedValues()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"codecheck-{Guid.NewGuid():N}.csv");
        var report = new ScanReport
        {
            Issues =
            [
                new Issue
                {
                    IssueId = "ISSUE-000001",
                    Severity = "Warning",
                    RuleId = "Quectel-CPP-001",
                    Engine = "CodeCheckBuiltin",
                    Language = "cpp",
                    File = "include/bad_header.hpp",
                    Line = 6,
                    BaselineState = "Existing",
                    SuppressionState = "Active",
                    Fingerprint = "sha256-stable-test",
                    Message = "Header should not use \"using namespace\", please fix."
                }
            ]
        };

        try
        {
            await new CsvReportWriter().WriteAsync(report, outputPath, CancellationToken.None);

            var csv = await File.ReadAllTextAsync(outputPath);
            Assert.Contains("IssueId,Severity,RuleId,Engine,Language,File,Line,BaselineState,SuppressionState,IsSuppressed,Fingerprint,Message", csv);
            Assert.Contains("ISSUE-000001", csv);
            Assert.Contains("sha256-stable-test", csv);
            Assert.Contains("\"Header should not use \"\"using namespace\"\", please fix.\"", csv);
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
