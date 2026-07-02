using CodeCheck.Core.Engines;
using CodeCheck.Core.Inputs;

namespace CodeCheck.Tests.Engines;

public sealed class BuiltinRuleRunnerTests
{
    [Fact]
    public async Task AnalyzeAsync_DetectsForbiddenCStringApis()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"codecheck-unsafe-{Guid.NewGuid():N}.c");
        await File.WriteAllLinesAsync(filePath,
        [
            "void unsafe_string_demo(char *dst)",
            "{",
            "    char buffer[32];",
            "    gets(buffer);",
            "    strcpy(dst, buffer);",
            "    sprintf(buffer, \"%s\", dst);",
            "}"
        ]);

        var files = new List<ScanInputFile>
        {
            new()
            {
                FullPath = filePath,
                RelativePath = Path.Combine("src", "unsafe_string.c"),
                Language = "c"
            }
        };

        try
        {
            var issues = await new BuiltinRuleRunner().AnalyzeAsync(files, CancellationToken.None);

            Assert.Contains(issues, issue => issue.RuleId == "Quectel-C-001");
            Assert.Contains(issues, issue => issue.RuleId == "Quectel-C-002");
            Assert.Contains(issues, issue => issue.RuleId == "Quectel-C-003");
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public async Task AnalyzeAsync_DetectsHeaderRules()
    {
        var filePath = Path.Combine(TestRepository.Root, "samples", "cpp-demo", "include", "bad_header.hpp");
        var files = new List<ScanInputFile>
        {
            new()
            {
                FullPath = filePath,
                RelativePath = Path.Combine("include", "bad_header.hpp"),
                Language = "cpp",
                IsHeader = true,
                IsExplicitInput = true
            }
        };

        var issues = await new BuiltinRuleRunner().AnalyzeAsync(files, CancellationToken.None);

        Assert.Contains(issues, issue => issue.RuleId == "Quectel-CPP-001");
        Assert.Contains(issues, issue => issue.RuleId == "Quectel-CPP-002");
    }
}
