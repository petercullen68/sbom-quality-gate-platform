using System.Text.Json;
using SbomQualityGate.Application.Exceptions;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Application.Models;
using SbomQualityGate.Domain.Enums;

namespace SbomQualityGate.Infrastructure.Validation;

public class SbomQsValidationTool(IProcessRunner processRunner) : IValidationTool
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
            // 🔥 replaced all Process code with abstraction
            var (exitCode, output, error) = await processRunner.RunAsync(
                "sbomqs",
                $"score {tempFile} --json",
                cancellationToken);

            if (exitCode != 0)
            {
                throw new ValidationToolException(
                    $"SBOMQS execution failed: {error}",
                    exitCode);
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
            // ✅ circuit breaker logic unchanged
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
