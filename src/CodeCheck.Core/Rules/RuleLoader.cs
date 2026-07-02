using System.Text.Json;

namespace CodeCheck.Core.Rules;

public interface IRuleLoader
{
    Task<RuleSet> LoadAsync(string ruleIndexPath, CancellationToken cancellationToken);
}

public sealed class RuleLoader : IRuleLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public async Task<RuleSet> LoadAsync(string ruleIndexPath, CancellationToken cancellationToken)
    {
        var indexFullPath = Path.GetFullPath(ruleIndexPath);
        var indexRoot = Path.GetDirectoryName(indexFullPath) ?? Directory.GetCurrentDirectory();
        var index = await ReadJsonAsync<RuleIndex>(indexFullPath, cancellationToken);

        var ruleSet = new RuleSet
        {
            RuleSetId = index.RuleSetId,
            Version = index.Version
        };

        foreach (var file in index.Files)
        {
            var ruleFilePath = ResolveRulePath(indexRoot, file);
            var ruleFile = await ReadJsonAsync<RuleFile>(ruleFilePath, cancellationToken);
            ruleSet.Rules.AddRange(ruleFile.Rules);
        }

        if (!string.IsNullOrWhiteSpace(index.Profiles))
        {
            var profilePath = ResolveRulePath(indexRoot, index.Profiles);
            var profileFile = await ReadJsonAsync<RuleProfileFile>(profilePath, cancellationToken);
            ruleSet.Profiles.AddRange(profileFile.Profiles);
        }

        return ruleSet;
    }

    private static async Task<T> ReadJsonAsync<T>(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, Options, cancellationToken)
            ?? throw new InvalidOperationException($"文件内容为空或格式不正确：{path}");
    }

    private static string ResolveRulePath(string indexRoot, string path)
    {
        if (Path.IsPathRooted(path))
        {
            return Path.GetFullPath(path);
        }

        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(indexRoot, path)),
            Path.GetFullPath(Path.Combine(indexRoot, "..", path)),
            Path.GetFullPath(path)
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return candidates[0];
    }
}
