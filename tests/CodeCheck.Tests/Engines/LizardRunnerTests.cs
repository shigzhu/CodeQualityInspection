using CodeCheck.Core.Engines;
using CodeCheck.Core.Inputs;
using CodeCheck.Core.Rules;

namespace CodeCheck.Tests.Engines;

public sealed class LizardRunnerTests
{
    [Fact]
    public async Task ParseIssues_CreatesComplexityAndFunctionLengthIssues()
    {
        var file = new ScanInputFile
        {
            FullPath = Path.Combine(TestRepository.Root, "samples", "cpp-demo", "src", "main.cpp"),
            RelativePath = Path.Combine("src", "main.cpp"),
            Language = "cpp"
        };
        var output = $" 120  18  200  2  143  ProcessMessage@{file.FullPath}:76";

        var issues = LizardRunner.ParseIssues(output, [file], await CreateMappingResolverAsync());

        Assert.Contains(issues, issue => issue.RuleId == "Quectel-CPP-037");
        Assert.Contains(issues, issue => issue.RuleId == "Quectel-CPP-038");
        Assert.Contains(issues, issue => issue.EngineRuleId == "cyclomatic-complexity");
        Assert.Contains(issues, issue => issue.EngineRuleId == "function-length");
    }

    private static async Task<RuleMappingResolver> CreateMappingResolverAsync()
    {
        var ruleSet = await new RuleLoader().LoadAsync(Path.Combine(TestRepository.Root, "rules", "rules.index.json"), CancellationToken.None);
        return new RuleMappingResolver(ruleSet.Mappings);
    }
}
