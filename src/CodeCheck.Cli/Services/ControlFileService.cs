using System.Text.Json;
using CodeCheck.Core.Configuration;

namespace CodeCheck.Cli.Services;

public sealed class ControlFileService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<bool> IsCancelRequestedAsync(CodeCheckConfig config, string rootDirectory, CancellationToken cancellationToken)
    {
        var command = await GetCommandAsync(config, rootDirectory, cancellationToken);
        return string.Equals(command, "cancel", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> WaitIfPausedAsync(CodeCheckConfig config, string rootDirectory, CancellationToken cancellationToken)
    {
        while (true)
        {
            var command = await GetCommandAsync(config, rootDirectory, cancellationToken);
            if (string.Equals(command, "cancel", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!string.Equals(command, "pause", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }
    }

    public async Task<string> GetCommandAsync(CodeCheckConfig config, string rootDirectory, CancellationToken cancellationToken)
    {
        var controlFile = ResolveControlFile(config, rootDirectory);
        if (!File.Exists(controlFile))
        {
            return string.Empty;
        }

        try
        {
            await using var stream = File.OpenRead(controlFile);
            var command = await JsonSerializer.DeserializeAsync<ControlFileCommand>(stream, Options, cancellationToken);
            return command?.Command ?? string.Empty;
        }
        catch (JsonException)
        {
            return string.Empty;
        }
        catch (IOException)
        {
            return string.Empty;
        }
    }

    private static string ResolveControlFile(CodeCheckConfig config, string rootDirectory)
    {
        var configuredPath = string.IsNullOrWhiteSpace(config.Runtime.ControlFile)
            ? Path.Combine(config.Runtime.TempDirectory, ".codecheck-control.json")
            : config.Runtime.ControlFile;

        return Path.GetFullPath(Path.IsPathRooted(configuredPath) ? configuredPath : Path.Combine(rootDirectory, configuredPath));
    }
}

public sealed class ControlFileCommand
{
    public string Command { get; set; } = string.Empty;
}
