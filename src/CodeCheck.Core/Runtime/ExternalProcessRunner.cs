using System.Diagnostics;

namespace CodeCheck.Core.Runtime;

public sealed class ExternalProcessRunner
{
    public async Task<ExternalProcessResult> RunAsync(string fileName, IEnumerable<string> arguments, string? workingDirectory, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ? Environment.CurrentDirectory : workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start process.");
        try
        {
            var waitTask = process.WaitForExitAsync(timeoutSource.Token);
            var stdoutTask = process.StandardOutput.ReadToEndAsync(timeoutSource.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(timeoutSource.Token);
            await waitTask;
            return new ExternalProcessResult(process.ExitCode, await stdoutTask, await stderrTask, false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            if (!process.HasExited) process.Kill(true);
            return new ExternalProcessResult(-1, string.Empty, "Process timed out.", true);
        }
    }
}

public sealed record ExternalProcessResult(int ExitCode, string StandardOutput, string StandardError, bool TimedOut)
{
    public bool Success => ExitCode == 0 && !TimedOut;
}
