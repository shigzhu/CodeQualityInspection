using System.Diagnostics;

namespace CodeCheck.Tests.Scripts;

public sealed class ReleaseScriptTests
{
    [Fact]
    public async Task BuildRelease_DefaultRequiresBundledTools()
    {
        var result = await RunPowerShellAsync(
            "scripts/build-release.ps1",
            "-Output",
            TempPath(),
            "-NoPublish");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("third_party_tools", result.Output, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BuildRelease_SkipToolsAllowsNoPublishDryRun()
    {
        var output = TempPath();

        try
        {
            var result = await RunPowerShellAsync(
                "scripts/build-release.ps1",
                "-Output",
                output,
                "-SkipTools",
                "-NoPublish");

            Assert.Equal(0, result.ExitCode);
            Assert.True(Directory.Exists(Path.Combine(output, "reports")));
            Assert.True(Directory.Exists(Path.Combine(output, "baseline")));
            Assert.True(Directory.Exists(Path.Combine(output, "suppressions")));
            Assert.True(Directory.Exists(Path.Combine(output, "logs")));
            Assert.True(Directory.Exists(Path.Combine(output, "temp")));
            Assert.True(File.Exists(Path.Combine(output, "tools", "third-party-versions.json")));
        }
        finally
        {
            if (Directory.Exists(output))
            {
                Directory.Delete(output, recursive: true);
            }
        }
    }

    [Fact]
    public async Task CheckRelease_ReturnsFailureWhenRequiredToolsAreMissing()
    {
        var releaseRoot = TempPath();
        Directory.CreateDirectory(releaseRoot);

        try
        {
            var result = await RunPowerShellAsync(
                "scripts/check-release.ps1",
                "-ReleaseRoot",
                releaseRoot);

            Assert.NotEqual(0, result.ExitCode);
            Assert.Contains("tools/llvm/bin/clang-tidy.exe", result.Output, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("tools/cppcheck/cppcheck.exe", result.Output, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("tools/lizard/lizard.exe", result.Output, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("tools/third-party-versions.json", result.Output, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(releaseRoot))
            {
                Directory.Delete(releaseRoot, recursive: true);
            }
        }
    }

    private static string TempPath() => Path.Combine(Path.GetTempPath(), $"codecheck-release-{Guid.NewGuid():N}");

    private static async Task<ScriptResult> RunPowerShellAsync(string scriptPath, params string[] arguments)
    {
        var fullScriptPath = Path.Combine(TestRepository.Root, scriptPath);
        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell",
            WorkingDirectory = TestRepository.Root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(fullScriptPath);
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start PowerShell.");
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return new ScriptResult(process.ExitCode, output + error);
    }

    private sealed record ScriptResult(int ExitCode, string Output);
}
