using System.IO;
using System.Text.Json;

namespace CodeCheck.Desktop;

public sealed class DesktopSettingsService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CodeCheck",
        "desktop-settings.json");

    public async Task<DesktopSettings> LoadAsync()
    {
        if (!File.Exists(SettingsPath))
        {
            return new DesktopSettings();
        }

        try
        {
            await using var stream = File.OpenRead(SettingsPath);
            return await JsonSerializer.DeserializeAsync<DesktopSettings>(stream, Options) ?? new DesktopSettings();
        }
        catch (JsonException)
        {
            return new DesktopSettings();
        }
        catch (IOException)
        {
            return new DesktopSettings();
        }
    }

    public async Task SaveAsync(DesktopSettings settings)
    {
        var directory = Path.GetDirectoryName(SettingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(SettingsPath);
        await JsonSerializer.SerializeAsync(stream, settings, Options);
    }
}

public sealed class DesktopSettings
{
    public string WorkspaceDirectory { get; set; } = string.Empty;
    public string CliPath { get; set; } = string.Empty;
    public string ConfigFile { get; set; } = string.Empty;
    public string StatusFile { get; set; } = string.Empty;
    public string ControlFile { get; set; } = string.Empty;
}
