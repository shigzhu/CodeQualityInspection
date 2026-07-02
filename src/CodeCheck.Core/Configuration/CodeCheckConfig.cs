using System.Text.Json.Serialization;

namespace CodeCheck.Core.Configuration;

public sealed class CodeCheckConfig
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("project")]
    public ProjectConfig Project { get; set; } = new();

    [JsonPropertyName("input")]
    public InputConfig Input { get; set; } = new();

    [JsonPropertyName("build")]
    public BuildConfig Build { get; set; } = new();

    [JsonPropertyName("scan")]
    public ScanConfig Scan { get; set; } = new();

    [JsonPropertyName("engines")]
    public EngineConfig Engines { get; set; } = new();

    [JsonPropertyName("rules")]
    public RuleConfig Rules { get; set; } = new();

    [JsonPropertyName("baseline")]
    public BaselineConfig Baseline { get; set; } = new();

    [JsonPropertyName("suppression")]
    public SuppressionConfig Suppression { get; set; } = new();

    [JsonPropertyName("report")]
    public ReportConfig Report { get; set; } = new();

    [JsonPropertyName("runtime")]
    public RuntimeConfig Runtime { get; set; } = new();
}

public sealed class ProjectConfig
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("root")]
    public string Root { get; set; } = string.Empty;

    [JsonPropertyName("projectKey")]
    public string ProjectKey { get; set; } = string.Empty;
}

public sealed class InputConfig
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "directory";

    [JsonPropertyName("paths")]
    public List<string> Paths { get; set; } = [];

    [JsonPropertyName("fileList")]
    public string FileList { get; set; } = string.Empty;

    [JsonPropertyName("sourceExtensions")]
    public List<string> SourceExtensions { get; set; } = [".c", ".cc", ".cpp", ".cxx"];

    [JsonPropertyName("headerExtensions")]
    public List<string> HeaderExtensions { get; set; } = [".h", ".hh", ".hpp", ".hxx"];

    [JsonPropertyName("headerScanPolicy")]
    public HeaderScanPolicyConfig HeaderScanPolicy { get; set; } = new();
}

public sealed class HeaderScanPolicyConfig
{
    [JsonPropertyName("scanHeadersInDirectoryMode")]
    public bool ScanHeadersInDirectoryMode { get; set; }

    [JsonPropertyName("allowHeaderAsExplicitInput")]
    public bool AllowHeaderAsExplicitInput { get; set; } = true;

    [JsonPropertyName("headerLanguageMode")]
    public string HeaderLanguageMode { get; set; } = "auto";
}

public sealed class BuildConfig
{
    [JsonPropertyName("includeDirectories")]
    public List<string> IncludeDirectories { get; set; } = [];

    [JsonPropertyName("defines")]
    public List<string> Defines { get; set; } = [];

    [JsonPropertyName("projectFiles")]
    public List<string> ProjectFiles { get; set; } = [];

    [JsonPropertyName("requireCompileContext")]
    public bool RequireCompileContext { get; set; } = true;

    [JsonPropertyName("allowDegradedScan")]
    public bool AllowDegradedScan { get; set; }

    [JsonPropertyName("cStandard")]
    public string CStandard { get; set; } = "c11";

    [JsonPropertyName("cppStandard")]
    public string CppStandard { get; set; } = "c++14";
}

public sealed class ScanConfig
{
    [JsonPropertyName("excludeDirectories")]
    public List<string> ExcludeDirectories { get; set; } = [];

    [JsonPropertyName("excludeFiles")]
    public List<string> ExcludeFiles { get; set; } = [];

    [JsonPropertyName("excludePatterns")]
    public List<string> ExcludePatterns { get; set; } = [];
}

public sealed class EngineConfig
{
    [JsonPropertyName("clangTidy")]
    public ToolConfig ClangTidy { get; set; } = new();

    [JsonPropertyName("cppcheck")]
    public ToolConfig Cppcheck { get; set; } = new();

    [JsonPropertyName("lizard")]
    public ToolConfig Lizard { get; set; } = new();
}

public sealed class ToolConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;
}

public sealed class RuleConfig
{
    [JsonPropertyName("ruleIndex")]
    public string RuleIndex { get; set; } = string.Empty;

    [JsonPropertyName("profile")]
    public string Profile { get; set; } = "default";
}

public sealed class BaselineConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "compare";

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("createIfMissing")]
    public bool CreateIfMissing { get; set; } = true;
}

public sealed class SuppressionConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;
}

public sealed class ReportConfig
{
    [JsonPropertyName("outputDirectory")]
    public string OutputDirectory { get; set; } = "reports";

    [JsonPropertyName("formats")]
    public List<string> Formats { get; set; } = ["json", "html", "sarif", "csv"];
}

public sealed class RuntimeConfig
{
    [JsonPropertyName("logDirectory")]
    public string LogDirectory { get; set; } = "logs";

    [JsonPropertyName("tempDirectory")]
    public string TempDirectory { get; set; } = "temp";

    [JsonPropertyName("controlFile")]
    public string ControlFile { get; set; } = "temp/.codecheck-control.json";

    [JsonPropertyName("statusFile")]
    public string StatusFile { get; set; } = "temp/scan.status.json";
}
