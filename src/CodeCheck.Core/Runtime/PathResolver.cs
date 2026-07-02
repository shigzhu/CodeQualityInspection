namespace CodeCheck.Core.Runtime;

public sealed class PathResolver
{
    public string RootDirectory { get; }

    public PathResolver(string? rootDirectory = null)
    {
        RootDirectory = Path.GetFullPath(rootDirectory ?? AppContext.BaseDirectory);
    }

    public string Resolve(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return RootDirectory;
        }

        return Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(Path.Combine(RootDirectory, path));
    }

    public static string GetRelativePath(string root, string path)
    {
        return Path.GetRelativePath(root, path).Replace('/', Path.DirectorySeparatorChar);
    }
}
