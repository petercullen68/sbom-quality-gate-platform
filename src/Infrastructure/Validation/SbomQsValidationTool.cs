using System.Diagnostics;
using System.Text.Json;
using SbomQualityGate.Application.Exceptions;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Application.Models;
using SbomQualityGate.Domain.Enums;

namespace SbomQualityGate.Infrastructure.Validation;

public class SbomQsValidationTool : IValidationTool
{
    public async Task<ValidationToolResult> ValidateAsync(
        string sbomJson,
        string profile,
        CancellationToken cancellationToken)
    {
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, sbomJson, cancellationToken);

        var psi = new ProcessStartInfo
        {
            FileName = "sbomqs",
            Arguments = $"score {tempFile} --json",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi)!;

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = await process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        File.Delete(tempFile);

        if (process.ExitCode != 0)
        {
            throw new ValidationToolException(
                $"SBOMQS execution failed: {error}",
                process.ExitCode);
        }

        // parse minimal fields (adjust once you see real output)
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

        return new ValidationToolResult
        {
            Status = status,
            Score = score,
            ReportJson = output
        };
    }
}
