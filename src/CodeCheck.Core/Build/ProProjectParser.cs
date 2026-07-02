using CodeCheck.Core.Runtime;

namespace CodeCheck.Core.Build;

public sealed class ProProjectParser
{
    public ProProjectInfo Parse(string projectFile, string rootDirectory)
    {
        var info = new ProProjectInfo();
        if (!File.Exists(projectFile))
        {
            return info;
        }

        var projectDirectory = Path.GetDirectoryName(projectFile) ?? rootDirectory;
        var resolver = new PathResolver(projectDirectory);
        foreach (var logicalLine in ReadLogicalLines(projectFile))
        {
            var line = RemoveComment(logicalLine).Trim();
            if (line.Length == 0)
            {
                continue;
            }

            AddValues(line, "INCLUDEPATH", value => info.IncludeDirectories.Add(ResolveProPath(value, resolver, projectDirectory)));
            AddValues(line, "DEFINES", value => info.Defines.Add(value));
        }

        info.IncludeDirectories = info.IncludeDirectories
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        info.Defines = info.Defines
            .Where(define => !string.IsNullOrWhiteSpace(define))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return info;
    }

    private static IEnumerable<string> ReadLogicalLines(string projectFile)
    {
        var current = string.Empty;
        foreach (var rawLine in File.ReadLines(projectFile))
        {
            var line = rawLine.TrimEnd();
            if (line.EndsWith('\\'))
            {
                current += line[..^1] + " ";
                continue;
            }

            yield return current + line;
            current = string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(current))
        {
            yield return current;
        }
    }

    private static string RemoveComment(string line)
    {
        var index = line.IndexOf('#', StringComparison.Ordinal);
        return index < 0 ? line : line[..index];
    }

    private static void AddValues(string line, string key, Action<string> addValue)
    {
        if (!line.StartsWith(key, StringComparison.Ordinal))
        {
            return;
        }

        var operatorIndex = line.IndexOf("+=", StringComparison.Ordinal);
        var operatorLength = 2;
        if (operatorIndex < 0)
        {
            operatorIndex = line.IndexOf('=');
            operatorLength = 1;
        }

        if (operatorIndex < 0)
        {
            return;
        }

        foreach (var value in line[(operatorIndex + operatorLength)..].Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries))
        {
            addValue(value.Trim());
        }
    }

    private static string ResolveProPath(string value, PathResolver resolver, string projectDirectory)
    {
        var normalized = value.Replace("$$PWD", projectDirectory, StringComparison.OrdinalIgnoreCase);
        return resolver.Resolve(normalized);
    }
}

public sealed class ProProjectInfo
{
    public List<string> IncludeDirectories { get; set; } = [];
    public List<string> Defines { get; set; } = [];
}
