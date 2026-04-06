using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Application.Models;
using SbomQualityGate.Domain.Enums;

namespace SbomQualityGate.IntegrationTests.Infrastructure;

public class StubSpecConformanceTool : ISpecConformanceTool
{
    public Task<SpecConformanceResult> CheckAsync(
        string sbomJson,
        string specType,
        string specVersion,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new SpecConformanceResult
        {
            Status = SpecConformanceStatus.Conformant,
            Violations = [],
            DeprecationWarnings = [],
            SchemaUrl = $"https://stub/schema/{specType}/{specVersion}",
            FetchedAt = DateTime.UtcNow
        });
    }
}
