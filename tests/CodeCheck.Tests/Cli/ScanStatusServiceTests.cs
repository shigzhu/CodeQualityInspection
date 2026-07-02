using System.Text.Json;
using CodeCheck.Cli.Services;
using CodeCheck.Core.Configuration;

namespace CodeCheck.Tests.Cli;

public sealed class ScanStatusServiceTests
{
    [Fact]
    public async Task WriteAsync_CreatesStatusFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"codecheck-status-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var statusFile = Path.Combine(directory, "scan.status.json");
        var config = new CodeCheckConfig
        {
            Runtime = new RuntimeConfig { StatusFile = statusFile }
        };

        try
        {
            await new ScanStatusService().WriteAsync(config, directory, new ScanStatus
            {
                Status = "Running",
                Phase = "EngineRunning",
                Engine = "cppcheck",
                TotalFiles = 3,
                TotalIssues = 2,
                FailedFiles = 1,
                Message = "Running cppcheck."
            }, CancellationToken.None);

            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(statusFile));
            var root = document.RootElement;
            Assert.Equal("Running", root.GetProperty("status").GetString());
            Assert.Equal("EngineRunning", root.GetProperty("phase").GetString());
            Assert.Equal("cppcheck", root.GetProperty("engine").GetString());
            Assert.Equal(3, root.GetProperty("totalFiles").GetInt32());
            Assert.Equal(2, root.GetProperty("totalIssues").GetInt32());
            Assert.Equal(1, root.GetProperty("failedFiles").GetInt32());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
