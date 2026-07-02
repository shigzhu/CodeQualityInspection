using CodeCheck.Core.Rules;

namespace CodeCheck.Tests.Rules;

public sealed class RuleMappingResolverTests
{
    [Fact]
    public async Task Resolve_UsesRuleMappingFile()
    {
        var ruleSet = await new RuleLoader().LoadAsync(
            Path.Combine(TestRepository.Root, "rules", "rules.index.json"),
            CancellationToken.None);
        var resolver = new RuleMappingResolver(ruleSet.Mappings);

        var mapped = resolver.Resolve("clang-tidy", "cppcoreguidelines-virtual-class-destructor", "cpp");

        Assert.Equal("Quectel-CPP-005", mapped.RuleId);
        Assert.Equal("cppcoreguidelines-virtual-class-destructor", mapped.EngineRuleId);
        Assert.False(mapped.IsFallback);
    }

    [Fact]
    public async Task Resolve_FallsBackByEngineAndLanguageWhenNoPatternMatches()
    {
        var ruleSet = await new RuleLoader().LoadAsync(
            Path.Combine(TestRepository.Root, "rules", "rules.index.json"),
            CancellationToken.None);
        var resolver = new RuleMappingResolver(ruleSet.Mappings);

        var mapped = resolver.Resolve("cppcheck", "unknown-checker", "c");

        Assert.Equal("Quectel-CERT-C-003", mapped.RuleId);
        Assert.Equal("unknown-checker", mapped.EngineRuleId);
        Assert.True(mapped.IsFallback);
    }
}
