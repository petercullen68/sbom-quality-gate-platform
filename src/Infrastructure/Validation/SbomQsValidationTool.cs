using System.Diagnostics;
using System.Text.Json;
using SbomQualityGate.Application.Exceptions;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Application.Models;
using SbomQualityGate.Domain.Enums;

namespace SbomQualityGate.Infrastructure.Validation;

public class SbomQsValidationTool : IValidationTool
{
    private int _failureCount;
    private DateTime _blockedUntil = DateTime.MinValue;

    public async Task<ValidationToolResult> ValidateAsync(
        string sbomJson,
        string profile,
        CancellationToken cancellationToken)
    {
        if (DateTime.UtcNow < _blockedUntil)
        {
            throw new InvalidOperationException("sbomqs temporarily unavailable (circuit open)");
        }

        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, sbomJson, cancellationToken);

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "sbomqs",
                Arguments = $"score {tempFile} --json",
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

                throw new TimeoutException("sbomqs execution timed out");
            }

            var output = await outputTask;
            var error = await errorTask;

            if (process.ExitCode != 0)
            {
                throw new ValidationToolException(
                    $"SBOMQS execution failed: {error}",
                    process.ExitCode);
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                throw new InvalidOperationException("SBOMQS returned empty output");
            }

            var doc = JsonDocument.Parse(output);
            var root = doc.RootElement;

            if (!root.TryGetProperty("files", out var files) || files.GetArrayLength() == 0)
            {
                throw new InvalidOperationException("SBOMQS output missing 'files' array");
            }

            var file = files[0];

            if (!file.TryGetProperty("sbom_quality_score", out var scoreElement))
            {
                throw new InvalidOperationException("SBOMQS output missing 'sbom_quality_score'");
            }

            var score = scoreElement.GetDouble();
            var status = score >= 80 ? ValidationStatus.Pass : ValidationStatus.Fail;

            // ✅ reset breaker on success
            _failureCount = 0;

            return new ValidationToolResult
            {
                Status = status,
                Score = score,
                ReportJson = output
            };
        }
        catch (Exception)
        {
            // ✅ track failures
            _failureCount++;

            if (_failureCount >= 5)
            {
                _blockedUntil = DateTime.UtcNow.AddMinutes(1);
                _failureCount = 0;
            }

            throw;
        }
        finally
        {
            try { File.Delete(tempFile); }
            catch
            {
                // ignored
            }
        }
    }
}
