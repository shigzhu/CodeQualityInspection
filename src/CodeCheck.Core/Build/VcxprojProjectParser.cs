using System.Xml.Linq;
using CodeCheck.Core.Runtime;

namespace CodeCheck.Core.Build;

public sealed class VcxprojProjectParser
{
    public VcxprojProjectInfo Parse(string projectFile, string rootDirectory)
    {
        var info = new VcxprojProjectInfo();
        if (!File.Exists(projectFile))
        {
            return info;
        }

        var projectDirectory = Path.GetDirectoryName(projectFile) ?? rootDirectory;
        var document = XDocument.Load(projectFile);

        foreach (var element in document.Descendants().Where(element => element.Name.LocalName == "AdditionalIncludeDirectories"))
        {
            foreach (var value in SplitMsBuildList(element.Value))
            {
                var includeDirectory = ResolveMsBuildPath(value, projectDirectory, rootDirectory);
                if (!string.IsNullOrWhiteSpace(includeDirectory))
                {
                    info.IncludeDirectories.Add(includeDirectory);
                }
            }
        }

        foreach (var element in document.Descendants().Where(element => element.Name.LocalName == "PreprocessorDefinitions"))
        {
            foreach (var value in SplitMsBuildList(element.Value))
            {
                if (!IsInheritedMacro(value) && !string.IsNullOrWhiteSpace(value))
                {
                    info.Defines.Add(value.Trim());
                }
            }
        }

        info.IncludeDirectories = info.IncludeDirectories
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        info.Defines = info.Defines
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return info;
    }

    private static IEnumerable<string> SplitMsBuildList(string value)
    {
        return value.Split([';'], StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim().Trim('"'));
    }

    private static string ResolveMsBuildPath(string value, string projectDirectory, string rootDirectory)
    {
        if (IsInheritedMacro(value) || value.Contains("$(", StringComparison.Ordinal))
        {
            value = value
                .Replace("$(ProjectDir)", EnsureTrailingSeparator(projectDirectory), StringComparison.OrdinalIgnoreCase)
                .Replace("$(SolutionDir)", EnsureTrailingSeparator(rootDirectory), StringComparison.OrdinalIgnoreCase);
        }

        if (IsInheritedMacro(value) || value.Contains("$(", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        return new PathResolver(projectDirectory).Resolve(value);
    }

    private static bool IsInheritedMacro(string value)
    {
        return value.Trim().StartsWith("%(", StringComparison.Ordinal);
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar) ? path : path + Path.DirectorySeparatorChar;
    }
}

public sealed class VcxprojProjectInfo
{
    public List<string> IncludeDirectories { get; set; } = [];
    public List<string> Defines { get; set; } = [];
}
