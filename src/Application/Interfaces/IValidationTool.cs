using SbomQualityGate.Application.Models;

namespace SbomQualityGate.Application.Interfaces;

public interface IValidationTool
{
    Task<ValidationToolResult> ValidateAsync(
        string sbomJson,
        string profile,
        CancellationToken cancellationToken);
}
