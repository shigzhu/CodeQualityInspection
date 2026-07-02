using CodeCheck.Core.Rules;

namespace CodeCheck.Tests.Rules;

public sealed class RuleLoaderTests
{
    [Fact]
    public async Task LoadAsync_LoadsInitialOneHundredRules()
    {
        var loader = new RuleLoader();
        var indexPath = Path.Combine(TestRepository.Root, "rules", "rules.index.json");

        var ruleSet = await loader.LoadAsync(indexPath, CancellationToken.None);

        Assert.Equal("CodeCheck-Quectel-C-Cpp", ruleSet.RuleSetId);
        Assert.Equal(100, ruleSet.Rules.Count);
        Assert.Contains(ruleSet.Profiles, profile => profile.Name == "default");
        Assert.Equal(ruleSet.Rules.Count, ruleSet.Rules.Select(rule => rule.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Contains(ruleSet.Rules, rule => rule.Id == "Quectel-C-021" && rule.Detection == "lizard");
        Assert.Contains(ruleSet.Rules, rule => rule.Id == "Quectel-CPP-038" && rule.Detection == "lizard");
    }
}
