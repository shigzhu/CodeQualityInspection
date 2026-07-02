using CodeCheck.Core.Configuration;
using CodeCheck.Core.Inputs;
using CodeCheck.Core.Runtime;

namespace CodeCheck.Core.Build;

public interface ICompileContextBuilder
{
    CompileContext Build(CodeCheckConfig config, IReadOnlyList<ScanInputFile> files, string rootDirectory);
}

public sealed class CompileContextBuilder : ICompileContextBuilder
{
    private readonly ProProjectParser _proProjectParser = new();
    private readonly VcxprojProjectParser _vcxprojProjectParser = new();

    public CompileContext Build(CodeCheckConfig config, IReadOnlyList<ScanInputFile> files, string rootDirectory)
    {
        var resolver = new PathResolver(rootDirectory);
        var includeDirectories = config.Build.IncludeDirectories
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => resolver.Resolve(path))
            .ToList();
        var defines = config.Build.Defines
            .Where(define => !string.IsNullOrWhiteSpace(define))
            .ToList();
        var projectFiles = config.Build.ProjectFiles
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => resolver.Resolve(path))
            .ToList();
        projectFiles.AddRange(DiscoverProjectFiles(config, resolver));

        foreach (var projectFile in projectFiles.Where(path => string.Equals(Path.GetExtension(path), ".pro", StringComparison.OrdinalIgnoreCase)))
        {
            var projectInfo = _proProjectParser.Parse(projectFile, rootDirectory);
            includeDirectories.AddRange(projectInfo.IncludeDirectories);
            defines.AddRange(projectInfo.Defines);
        }

        foreach (var projectFile in projectFiles.Where(path => string.Equals(Path.GetExtension(path), ".vcxproj", StringComparison.OrdinalIgnoreCase)))
        {
            var projectInfo = _vcxprojProjectParser.Parse(projectFile, rootDirectory);
            includeDirectories.AddRange(projectInfo.IncludeDirectories);
            defines.AddRange(projectInfo.Defines);
        }

        return new CompileContext
        {
            IncludeDirectories = includeDirectories
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Defines = defines
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            ProjectFiles = projectFiles
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            CStandard = config.Build.CStandard,
            CppStandard = config.Build.CppStandard
        };
    }

    private static IEnumerable<string> DiscoverProjectFiles(CodeCheckConfig config, PathResolver resolver)
    {
        foreach (var inputPath in config.Input.Paths.Where(path => !string.IsNullOrWhiteSpace(path)))
        {
            var fullPath = resolver.Resolve(inputPath);
            if (!Directory.Exists(fullPath))
            {
                continue;
            }

            foreach (var projectFile in Directory.EnumerateFiles(fullPath, "*.vcxproj", SearchOption.AllDirectories))
            {
                yield return projectFile;
            }

            foreach (var projectFile in Directory.EnumerateFiles(fullPath, "*.pro", SearchOption.AllDirectories))
            {
                yield return projectFile;
            }
        }
    }
}
