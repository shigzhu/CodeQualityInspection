using System.Text.Json.Serialization;

namespace CodeCheck.Core.Rules;

public sealed class RuleDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("sourceRuleId")]
    public string SourceRuleId { get; set; } = string.Empty;

    [JsonPropertyName("language")]
    public List<string> Language { get; set; } = [];

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty;

    [JsonPropertyName("detection")]
    public string Detection { get; set; } = string.Empty;

    [JsonPropertyName("defaultEnabled")]
    public bool DefaultEnabled { get; set; } = true;

    [JsonPropertyName("allowDisable")]
    public bool AllowDisable { get; set; } = true;

    [JsonPropertyName("disableRisk")]
    public string DisableRisk { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("suggestion")]
    public string Suggestion { get; set; } = string.Empty;
}

public sealed class RuleFile
{
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = "1.0.0";

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("rules")]
    public List<RuleDefinition> Rules { get; set; } = [];
}

public sealed class RuleIndex
{
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = "1.0.0";

    [JsonPropertyName("ruleSetId")]
    public string RuleSetId { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("files")]
    public List<string> Files { get; set; } = [];

    [JsonPropertyName("profiles")]
    public string Profiles { get; set; } = string.Empty;

    [JsonPropertyName("mapping")]
    public string Mapping { get; set; } = string.Empty;
}

public sealed class RuleProfileFile
{
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = "1.0.0";

    [JsonPropertyName("profiles")]
    public List<RuleProfile> Profiles { get; set; } = [];
}

public sealed class RuleProfile
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("enabledRuleIds")]
    public List<string> EnabledRuleIds { get; set; } = [];
}

public sealed class RuleSet
{
    public string RuleSetId { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public List<RuleDefinition> Rules { get; set; } = [];
    public List<RuleProfile> Profiles { get; set; } = [];
}
