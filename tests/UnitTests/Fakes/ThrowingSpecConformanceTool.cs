using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Application.Models;

namespace SbomQualityGate.UnitTests.Fakes;

public class ThrowingSpecConformanceTool : ISpecConformanceTool
{
    public Task<SpecConformanceResult> CheckAsync(
        string sbomJson,
        string specType,
        string specVersion,
        CancellationToken cancellationToken)
        => throw new InvalidOperationException("Simulated spec conformance failure");
}
