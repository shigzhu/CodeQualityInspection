using CodeCheck.Core.Configuration;
using CodeCheck.Core.Runtime;

namespace CodeCheck.Core.Inputs;

public interface IFileDiscoveryService
{
    Task<IReadOnlyList<ScanInputFile>> DiscoverAsync(CodeCheckConfig config, CancellationToken cancellationToken);
}

public sealed class FileDiscoveryService : IFileDiscoveryService
{
    public Task<IReadOnlyList<ScanInputFile>> DiscoverAsync(CodeCheckConfig config, CancellationToken cancellationToken)
    {
        var root = GetProjectRoot(config);
        var resolver = new PathResolver(Directory.GetCurrentDirectory());
        var files = new List<ScanInputFile>();

        if (string.Equals(config.Input.Type, "file-list", StringComparison.OrdinalIgnoreCase))
        {
            AddFileList(config, resolver, root, files);
        }
        else
        {
            foreach (var path in config.Input.Paths)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var fullPath = resolver.Resolve(path);
                if (Directory.Exists(fullPath))
                {
                    AddDirectory(config, fullPath, root, files);
                }
                else if (File.Exists(fullPath))
                {
                    AddFile(config, fullPath, root, files, isExplicitInput: true);
                }
            }
        }

        return Task.FromResult<IReadOnlyList<ScanInputFile>>(files
            .GroupBy(x => x.FullPath, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.First())
            .OrderBy(x => x.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList());
    }

    private static void AddDirectory(CodeCheckConfig config, string directory, string root, List<ScanInputFile> files)
    {
        foreach (var file in Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories))
        {
            if (IsExcluded(config, file))
            {
                continue;
            }

            AddFile(config, file, root, files, isExplicitInput: false);
        }
    }

    private static void AddFileList(CodeCheckConfig config, PathResolver resolver, string root, List<ScanInputFile> files)
    {
        var listPath = resolver.Resolve(config.Input.FileList);
        foreach (var line in File.ReadLines(listPath))
        {
            var path = line.Trim();
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            var fullPath = resolver.Resolve(path);
            if (File.Exists(fullPath))
            {
                AddFile(config, fullPath, root, files, isExplicitInput: true);
            }
        }
    }

    private static void AddFile(CodeCheckConfig config, string file, string root, List<ScanInputFile> files, bool isExplicitInput)
    {
        var extension = Path.GetExtension(file).ToLowerInvariant();
        var isSource = config.Input.SourceExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        var isHeader = config.Input.HeaderExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);

        if (!isSource && !isHeader)
        {
            return;
        }

        if (isHeader && !isExplicitInput && !config.Input.HeaderScanPolicy.ScanHeadersInDirectoryMode)
        {
            return;
        }

        if (isHeader && isExplicitInput && !config.Input.HeaderScanPolicy.AllowHeaderAsExplicitInput)
        {
            return;
        }

        files.Add(new ScanInputFile
        {
            FullPath = Path.GetFullPath(file),
            RelativePath = PathResolver.GetRelativePath(root, file),
            Language = DetectLanguage(extension, config.Input.HeaderScanPolicy.HeaderLanguageMode),
            IsHeader = isHeader,
            IsExplicitInput = isExplicitInput
        });
    }

    private static bool IsExcluded(CodeCheckConfig config, string file)
    {
        var parts = file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        if (parts.Any(part => config.Scan.ExcludeDirectories.Contains(part, StringComparer.OrdinalIgnoreCase)))
        {
            return true;
        }

        var fileName = Path.GetFileName(file);
        return config.Scan.ExcludeFiles.Any(pattern => MatchesWildcard(fileName, pattern));
    }

    private static bool MatchesWildcard(string value, string pattern)
    {
        if (string.Equals(value, pattern, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!pattern.Contains('*'))
        {
            return false;
        }

        var parts = pattern.Split('*', StringSplitOptions.RemoveEmptyEntries);
        var position = 0;
        foreach (var part in parts)
        {
            var index = value.IndexOf(part, position, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return false;
            }

            position = index + part.Length;
        }

        return true;
    }

    private static string DetectLanguage(string extension, string headerLanguageMode)
    {
        if (extension == ".c")
        {
            return "c";
        }

        if (extension is ".cpp" or ".cc" or ".cxx" or ".hpp" or ".hh" or ".hxx")
        {
            return "cpp";
        }

        return headerLanguageMode.Equals("c", StringComparison.OrdinalIgnoreCase) ? "c" : "cpp";
    }

    private static string GetProjectRoot(CodeCheckConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.Project.Root))
        {
            return Path.GetFullPath(config.Project.Root);
        }

        var firstPath = config.Input.Paths.FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(firstPath))
        {
            var fullPath = Path.GetFullPath(firstPath);
            return Directory.Exists(fullPath) ? fullPath : Path.GetDirectoryName(fullPath) ?? Directory.GetCurrentDirectory();
        }

        return Directory.GetCurrentDirectory();
    }
}
