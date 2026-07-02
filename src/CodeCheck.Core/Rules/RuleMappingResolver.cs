namespace CodeCheck.Core.Rules;

public sealed class RuleMappingResolver
{
    private readonly IReadOnlyList<RuleMapping> _mappings;

    public RuleMappingResolver(IReadOnlyList<RuleMapping> mappings)
    {
        _mappings = mappings;
    }

    public RuleMappingResult Resolve(string engine, string engineRuleId, string language)
    {
        var exact = _mappings.FirstOrDefault(mapping =>
            !mapping.Fallback &&
            MatchesEngine(mapping, engine) &&
            MatchesLanguage(mapping, language) &&
            MatchesRule(mapping, engineRuleId));

        if (exact is not null)
        {
            return new RuleMappingResult
            {
                RuleId = exact.RuleId,
                EngineRuleId = engineRuleId,
                IsFallback = false
            };
        }

        var fallback = _mappings.FirstOrDefault(mapping =>
            mapping.Fallback &&
            MatchesEngine(mapping, engine) &&
            MatchesLanguage(mapping, language));

        return new RuleMappingResult
        {
            RuleId = fallback?.RuleId ?? string.Empty,
            EngineRuleId = engineRuleId,
            IsFallback = true
        };
    }

    private static bool MatchesEngine(RuleMapping mapping, string engine)
    {
        return string.Equals(mapping.Engine, engine, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesLanguage(RuleMapping mapping, string language)
    {
        return string.IsNullOrWhiteSpace(mapping.Language) ||
            string.Equals(mapping.Language, language, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesRule(RuleMapping mapping, string engineRuleId)
    {
        return mapping.Match switch
        {
            "contains" => engineRuleId.Contains(mapping.EngineRuleId, StringComparison.OrdinalIgnoreCase),
            _ => string.Equals(mapping.EngineRuleId, engineRuleId, StringComparison.OrdinalIgnoreCase)
        };
    }
}
