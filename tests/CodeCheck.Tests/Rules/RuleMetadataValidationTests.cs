using System.Text.Json;

namespace CodeCheck.Tests.Rules;

public sealed class RuleMetadataValidationTests
{
    private static readonly string[] RuleFiles =
    [
        Path.Combine(TestRepository.Root, "rules", "quectel-c-rules.json"),
        Path.Combine(TestRepository.Root, "rules", "quectel-cpp-rules.json"),
        Path.Combine(TestRepository.Root, "rules", "cert-c-rules.json"),
        Path.Combine(TestRepository.Root, "rules", "cert-cpp-rules.json")
    ];

    private static readonly HashSet<string> AllowedDetectionMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "builtin",
        "clang-tidy",
        "cppcheck",
        "lizard",
        "manual"
    };

    [Fact]
    public void RuleFiles_ContainExactlyOneHundredRules()
    {
        var rules = LoadRules();

        Assert.Equal(100, rules.Count);
    }

    [Fact]
    public void RuleFiles_HaveUniqueNonEmptyIds()
    {
        var rules = LoadRules();
        var ids = rules.Select(rule => rule.GetProperty("id").GetString()).ToList();

        Assert.DoesNotContain(ids, string.IsNullOrWhiteSpace);
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void RuleFiles_UseAllowedDetectionMethods()
    {
        var rules = LoadRules();

        foreach (var rule in rules)
        {
            var id = rule.GetProperty("id").GetString();
            var detection = rule.GetProperty("detection").GetString();

            Assert.True(
                detection is not null && AllowedDetectionMethods.Contains(detection),
                $"Rule {id} has invalid detection method '{detection}'.");
        }
    }

    [Fact]
    public void RuleFiles_ContainRequiredDisplayFields()
    {
        var rules = LoadRules();

        foreach (var rule in rules)
        {
            var id = rule.GetProperty("id").GetString();

            Assert.False(string.IsNullOrWhiteSpace(rule.GetProperty("title").GetString()), $"Rule {id} missing title.");
            Assert.False(string.IsNullOrWhiteSpace(rule.GetProperty("source").GetString()), $"Rule {id} missing source.");
            Assert.False(string.IsNullOrWhiteSpace(rule.GetProperty("severity").GetString()), $"Rule {id} missing severity.");
            Assert.False(string.IsNullOrWhiteSpace(rule.GetProperty("description").GetString()), $"Rule {id} missing description.");
            Assert.False(string.IsNullOrWhiteSpace(rule.GetProperty("suggestion").GetString()), $"Rule {id} missing suggestion.");
        }
    }

    private static List<JsonElement> LoadRules()
    {
        var rules = new List<JsonElement>();

        foreach (var ruleFile in RuleFiles)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(ruleFile));
            foreach (var rule in document.RootElement.GetProperty("rules").EnumerateArray())
            {
                rules.Add(rule.Clone());
            }
        }

        return rules;
    }
}
