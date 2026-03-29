using SbomQualityGate.Application.Interfaces;

namespace SbomQualityGate.Infrastructure.Process;
using System.Diagnostics;

public class ProcessRunner : IProcessRunner
{
    public async Task<(int ExitCode, string Output, string Error)> RunAsync(
        string fileName,
        string arguments,
        CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process();
        process.StartInfo = psi;

        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        var waitForExitTask = process.WaitForExitAsync(cancellationToken);
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);

        var completed = await Task.WhenAny(waitForExitTask, timeoutTask);

        if (completed != waitForExitTask)
        {
            try { process.Kill(true); }
            catch
            {
                // ignored
            }

            throw new TimeoutException("Process execution timed out");
        }

        var output = await outputTask;
        var error = await errorTask;

        return (process.ExitCode, output, error);
    }
}
