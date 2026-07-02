using CodeCheck.Core.Build;
using CodeCheck.Core.Runtime;

namespace CodeCheck.Core.Configuration;

public interface IConfigValidator
{
    ValidationResult Validate(CodeCheckConfig config, string configPath, bool isScan);
}

public sealed class ConfigValidator : IConfigValidator
{
    private static readonly HashSet<string> AllowedReportFormats = new(StringComparer.OrdinalIgnoreCase)
    {
        "json",
        "html",
        "sarif",
        "csv"
    };

    private readonly CompileContextBuilder _compileContextBuilder = new();

    public ValidationResult Validate(CodeCheckConfig config, string configPath, bool isScan)
    {
        var result = new ValidationResult();
        var root = Directory.GetCurrentDirectory();
        var resolver = new PathResolver(root);

        if (string.IsNullOrWhiteSpace(config.Version))
        {
            result.AddError("缺少 version 配置。");
        }

        if (string.IsNullOrWhiteSpace(config.Rules.RuleIndex))
        {
            result.AddError("缺少 rules.ruleIndex 配置。");
        }
        else if (!File.Exists(resolver.Resolve(config.Rules.RuleIndex)))
        {
            result.AddError($"规则索引文件不存在：{config.Rules.RuleIndex}");
        }

        if (string.IsNullOrWhiteSpace(config.Rules.Profile))
        {
            result.AddError("缺少 rules.profile 配置。");
        }

        ValidateTool(config.Engines.ClangTidy, "clang-tidy", resolver, result);
        ValidateTool(config.Engines.Cppcheck, "cppcheck", resolver, result);
        ValidateTool(config.Engines.Lizard, "lizard", resolver, result);

        EnsureDirectory(config.Report.OutputDirectory, "report.outputDirectory", resolver, result);
        EnsureDirectory(config.Runtime.TempDirectory, "runtime.tempDirectory", resolver, result);
        EnsureDirectory(config.Runtime.LogDirectory, "runtime.logDirectory", resolver, result);
        ValidateBuildStandards(config.Build, result);
        ValidateReportFormats(config.Report, result);

        if (isScan)
        {
            ValidateScanInput(config, resolver, result);

            if (config.Build.RequireCompileContext &&
                !config.Build.AllowDegradedScan &&
                config.Build.IncludeDirectories.Count == 0 &&
                config.Build.ProjectFiles.Count == 0)
            {
                result.AddError("缺少 include 路径或工程文件，且不允许降级扫描。");
            }
        }

        return result;
    }

    private static void ValidateTool(ToolConfig tool, string name, PathResolver resolver, ValidationResult result)
    {
        if (!tool.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(tool.Path))
        {
            result.AddError($"已启用 {name}，但未配置工具路径。");
            return;
        }

        if (!File.Exists(resolver.Resolve(tool.Path)))
        {
            result.AddError($"{name} 工具不存在：{tool.Path}");
        }
    }

    private static void EnsureDirectory(string path, string name, PathResolver resolver, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            result.AddError($"缺少 {name} 配置。");
            return;
        }

        try
        {
            Directory.CreateDirectory(resolver.Resolve(path));
        }
        catch (Exception ex)
        {
            result.AddError($"目录不可创建或不可写：{path}。{ex.Message}");
        }
    }

    private static void ValidateScanInput(CodeCheckConfig config, PathResolver resolver, ValidationResult result)
    {
        if (config.Input.Paths.Count == 0 && string.IsNullOrWhiteSpace(config.Input.FileList))
        {
            result.AddError("扫描输入为空，请配置 input.paths 或 input.fileList。");
            return;
        }

        foreach (var path in config.Input.Paths)
        {
            var fullPath = resolver.Resolve(path);
            if (!File.Exists(fullPath) && !Directory.Exists(fullPath))
            {
                result.AddError($"输入路径不存在：{path}");
            }
        }

        if (!string.IsNullOrWhiteSpace(config.Input.FileList) && !File.Exists(resolver.Resolve(config.Input.FileList)))
        {
            result.AddError($"文件清单不存在：{config.Input.FileList}");
        }
    }

    private static void ValidateBuildStandards(BuildConfig build, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(build.CStandard))
        {
            result.AddError("缺少 build.cStandard 配置。");
        }

        if (string.IsNullOrWhiteSpace(build.CppStandard))
        {
            result.AddError("缺少 build.cppStandard 配置。");
        }
    }

    private static void ValidateReportFormats(ReportConfig report, ValidationResult result)
    {
        if (report.Formats.Count == 0)
        {
            result.AddError("缺少 report.formats 配置。");
            return;
        }

        foreach (var format in report.Formats)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                result.AddError("report.formats 包含空格式。");
                continue;
            }

            if (!AllowedReportFormats.Contains(format))
            {
                result.AddError($"V1 不支持报告格式：{format}");
            }
        }
    }
}
