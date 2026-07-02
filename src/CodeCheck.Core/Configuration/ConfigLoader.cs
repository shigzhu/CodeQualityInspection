using System.Text.Json;

namespace CodeCheck.Core.Configuration;

public interface IConfigLoader
{
    Task<CodeCheckConfig> LoadAsync(string path, CancellationToken cancellationToken);
}

public sealed class ConfigLoader : IConfigLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public async Task<CodeCheckConfig> LoadAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<CodeCheckConfig>(stream, Options, cancellationToken)
            ?? throw new InvalidOperationException("配置文件为空或格式不正确。");
    }
}
