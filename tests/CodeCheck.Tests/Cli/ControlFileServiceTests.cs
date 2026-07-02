using CodeCheck.Cli.Services;
using CodeCheck.Core.Configuration;

namespace CodeCheck.Tests.Cli;

public sealed class ControlFileServiceTests
{
    [Fact]
    public async Task IsCancelRequestedAsync_ReturnsTrueForCancelCommand()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"codecheck-control-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var controlFile = Path.Combine(directory, ".codecheck-control.json");
        await File.WriteAllTextAsync(controlFile, "{ \"command\": \"cancel\" }");
        var config = new CodeCheckConfig
        {
            Runtime = new RuntimeConfig { ControlFile = controlFile }
        };

        try
        {
            var cancelled = await new ControlFileService().IsCancelRequestedAsync(config, directory, CancellationToken.None);

            Assert.True(cancelled);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task IsCancelRequestedAsync_ReturnsFalseForMissingOrInvalidFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"codecheck-control-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var config = new CodeCheckConfig
        {
            Runtime = new RuntimeConfig { ControlFile = Path.Combine(directory, ".codecheck-control.json") }
        };

        try
        {
            var cancelled = await new ControlFileService().IsCancelRequestedAsync(config, directory, CancellationToken.None);

            Assert.False(cancelled);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task WaitIfPausedAsync_ReturnsFalseForResumeCommand()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"codecheck-control-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var controlFile = Path.Combine(directory, ".codecheck-control.json");
        await File.WriteAllTextAsync(controlFile, "{ \"command\": \"resume\" }");
        var config = new CodeCheckConfig
        {
            Runtime = new RuntimeConfig { ControlFile = controlFile }
        };

        try
        {
            var cancelled = await new ControlFileService().WaitIfPausedAsync(config, directory, CancellationToken.None);

            Assert.False(cancelled);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task WaitIfPausedAsync_ReturnsTrueWhenPauseChangesToCancel()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"codecheck-control-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var controlFile = Path.Combine(directory, ".codecheck-control.json");
        await File.WriteAllTextAsync(controlFile, "{ \"command\": \"pause\" }");
        var config = new CodeCheckConfig
        {
            Runtime = new RuntimeConfig { ControlFile = controlFile }
        };

        try
        {
            var waitTask = new ControlFileService().WaitIfPausedAsync(config, directory, CancellationToken.None);
            await Task.Delay(100);
            await File.WriteAllTextAsync(controlFile, "{ \"command\": \"cancel\" }");

            var cancelled = await waitTask;

            Assert.True(cancelled);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
