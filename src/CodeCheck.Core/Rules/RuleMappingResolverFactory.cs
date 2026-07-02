namespace CodeCheck.Core.Rules;

public static class RuleMappingResolverFactory
{
    public static RuleMappingResolver CreateDefault()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "rules", "rule-mapping.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "rules", "rule-mapping.json")
        };

        var mappingPath = candidates.FirstOrDefault(File.Exists);
        if (mappingPath is null)
        {
            return new RuleMappingResolver([]);
        }

        var mappingFile = System.Text.Json.JsonSerializer.Deserialize<RuleMappingFile>(
            File.ReadAllText(mappingPath),
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            });

        return new RuleMappingResolver(mappingFile?.Mappings ?? []);
    }
}
