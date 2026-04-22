using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Application.Models;

namespace SbomQualityGate.UnitTests.Fakes;

/// <summary>
/// Simulates a CancellationToken cancellation during CheckAsync.
/// Used to verify that worker shutdown does not mutate job state.
/// </summary>
public class CancellingSpecConformanceTool : ISpecConformanceTool
{
    public Task<SpecConformanceResult> CheckAsync(
        string sbomJson,
        string specType,
        string specVersion,
        CancellationToken cancellationToken)
    {
        throw new OperationCanceledException(cancellationToken);
    }
}
