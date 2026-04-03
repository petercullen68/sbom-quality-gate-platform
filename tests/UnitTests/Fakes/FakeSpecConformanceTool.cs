using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Application.Models;

namespace SbomQualityGate.UnitTests.Fakes;

public class FakeSpecConformanceTool : ISpecConformanceTool
{
    public bool WasCalled { get; private set; }
    public string? LastSpecType { get; private set; }
    public string? LastSpecVersion { get; private set; }

    public SpecConformanceResult ResultToReturn { get; init; } = new()
    {
        IsConformant = true,
        Violations = [],
        DeprecationWarnings = [],
        SchemaUrl = "https://example.com/schema.json",
        FetchedAt = DateTime.UtcNow
    };

    public Task<SpecConformanceResult> CheckAsync(
        string sbomJson,
        string specType,
        string specVersion,
        CancellationToken cancellationToken)
    {
        WasCalled = true;
        LastSpecType = specType;
        LastSpecVersion = specVersion;
        return Task.FromResult(ResultToReturn);
    }
}
