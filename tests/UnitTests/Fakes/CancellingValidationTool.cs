using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Application.Models;

namespace SbomQualityGate.UnitTests.Fakes;

/// <summary>
/// Simulates a CancellationToken cancellation during ValidateAsync.
/// Used to verify that worker shutdown does not mutate job state.
/// </summary>
public class CancellingValidationTool : IValidationTool
{
    public Task<ValidationToolResult> ValidateAsync(
        string sbomJson,
        string profile,
        CancellationToken cancellationToken)
    {
        throw new OperationCanceledException(cancellationToken);
    }
}
