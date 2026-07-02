using System.Text.Json;
using CodeCheck.Core.Configuration;

namespace CodeCheck.Tests.Configuration;

public sealed class ConfigValidatorTests
{
    [Fact]
    public void DefaultConfig_DeclaresV1LanguageStandardsAndReportFormats()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestRepository.Root, "configs", "default-codecheck.json")));
        var root = document.RootElement;

        var build = root.GetProperty("build");
        Assert.Equal("c11", build.GetProperty("cStandard").GetString());
        Assert.Equal("c++14", build.GetProperty("cppStandard").GetString());

        var formats = root.GetProperty("report").GetProperty("formats")
            .EnumerateArray()
            .Select(format => format.GetString())
            .ToList();

        Assert.Equal(["json", "html", "sarif", "csv"], formats);
        Assert.DoesNotContain("xlsx", formats, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_DefaultTemplate_IsValidForValidateCommandWhenToolsDisabled()
    {
        var config = new CodeCheckConfig
        {
            Rules = new RuleConfig
            {
                RuleIndex = Path.Combine(TestRepository.Root, "rules", "rules.index.json"),
                Profile = "default"
            },
            Engines = new EngineConfig
            {
                ClangTidy = new ToolConfig { Enabled = false },
                Cppcheck = new ToolConfig { Enabled = false },
                Lizard = new ToolConfig { Enabled = false }
            },
            Report = new ReportConfig { OutputDirectory = Path.Combine(TestRepository.Root, "temp", "test-reports") },
            Runtime = new RuntimeConfig
            {
                LogDirectory = Path.Combine(TestRepository.Root, "temp", "test-logs"),
                TempDirectory = Path.Combine(TestRepository.Root, "temp", "test-temp")
            }
        };

        var result = new ConfigValidator().Validate(config, "", isScan: false);

        Assert.True(result.IsValid, string.Join(Environment.NewLine, result.Errors));
    }

    [Fact]
    public void Validate_ReportFormatsRejectsDeferredXlsx()
    {
        var config = new CodeCheckConfig
        {
            Rules = new RuleConfig
            {
                RuleIndex = Path.Combine(TestRepository.Root, "rules", "rules.index.json"),
                Profile = "default"
            },
            Engines = new EngineConfig
            {
                ClangTidy = new ToolConfig { Enabled = false },
                Cppcheck = new ToolConfig { Enabled = false },
                Lizard = new ToolConfig { Enabled = false }
            },
            Report = new ReportConfig
            {
                OutputDirectory = Path.Combine(TestRepository.Root, "temp", "test-reports"),
                Formats = ["json", "xlsx"]
            },
            Runtime = new RuntimeConfig
            {
                LogDirectory = Path.Combine(TestRepository.Root, "temp", "test-logs"),
                TempDirectory = Path.Combine(TestRepository.Root, "temp", "test-temp")
            }
        };

        var result = new ConfigValidator().Validate(config, "", isScan: false);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("xlsx", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ScanWithoutCompileContext_ReturnsConfigError()
    {
        var config = new CodeCheckConfig
        {
            Input = new InputConfig
            {
                Type = "directory",
                Paths = [Path.Combine(TestRepository.Root, "samples", "c-demo")]
            },
            Build = new BuildConfig
            {
                RequireCompileContext = true,
                AllowDegradedScan = false
            },
            Rules = new RuleConfig
            {
                RuleIndex = Path.Combine(TestRepository.Root, "rules", "rules.index.json"),
                Profile = "default"
            },
            Engines = new EngineConfig
            {
                ClangTidy = new ToolConfig { Enabled = false },
                Cppcheck = new ToolConfig { Enabled = false },
                Lizard = new ToolConfig { Enabled = false }
            },
            Report = new ReportConfig { OutputDirectory = Path.Combine(TestRepository.Root, "temp", "test-reports") },
            Runtime = new RuntimeConfig
            {
                LogDirectory = Path.Combine(TestRepository.Root, "temp", "test-logs"),
                TempDirectory = Path.Combine(TestRepository.Root, "temp", "test-temp")
            }
        };

        var result = new ConfigValidator().Validate(config, "", isScan: true);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Contains("include", StringComparison.OrdinalIgnoreCase));
    }
}
