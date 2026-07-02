using CodeCheck.Core.Engines;
using CodeCheck.Core.Inputs;

namespace CodeCheck.Tests.Engines;

public sealed class LizardRunnerTests
{
    [Fact]
    public void ParseIssues_CreatesComplexityAndFunctionLengthIssues()
    {
        var file = new ScanInputFile
        {
            FullPath = Path.Combine(TestRepository.Root, "samples", "cpp-demo", "src", "main.cpp"),
            RelativePath = Path.Combine("src", "main.cpp"),
            Language = "cpp"
        };
        var output = $" 120  18  200  2  143  ProcessMessage@{file.FullPath}:76";

        var issues = LizardRunner.ParseIssues(output, [file]);

        Assert.Contains(issues, issue => issue.RuleId == "Quectel-CPP-037");
        Assert.Contains(issues, issue => issue.RuleId == "Quectel-CPP-038");
    }
}
