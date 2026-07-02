using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeCheck.Core.Configuration;

namespace CodeCheck.Cli.Services;

public sealed class ScanStatusService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task WriteAsync(CodeCheckConfig config, string rootDirectory, ScanStatus status, CancellationToken cancellationToken)
    {
        status.UpdatedAt = DateTime.Now;
        var statusFile = ResolveStatusFile(config, rootDirectory);
        var directory = Path.GetDirectoryName(statusFile);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(statusFile);
        await JsonSerializer.SerializeAsync(stream, status, Options, cancellationToken);
    }

    private static string ResolveStatusFile(CodeCheckConfig config, string rootDirectory)
    {
        var configuredPath = string.IsNullOrWhiteSpace(config.Runtime.StatusFile)
            ? Path.Combine(config.Runtime.TempDirectory, "scan.status.json")
            : config.Runtime.StatusFile;

        return Path.GetFullPath(Path.IsPathRooted(configuredPath) ? configuredPath : Path.Combine(rootDirectory, configuredPath));
    }
}

public sealed class ScanStatus
{
    public string Status { get; set; } = string.Empty;
    public string Phase { get; set; } = string.Empty;
    public string Engine { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int TotalFiles { get; set; }
    public int TotalIssues { get; set; }
    public int FailedFiles { get; set; }
    public string ReportPath { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
