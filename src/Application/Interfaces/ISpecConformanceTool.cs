using SbomQualityGate.Application.Models;

namespace SbomQualityGate.Application.Interfaces;

public interface ISpecConformanceTool
{
    Task<SpecConformanceResult> CheckAsync(
        string sbomJson,
        string specType,
        string specVersion,
        CancellationToken cancellationToken);
}
