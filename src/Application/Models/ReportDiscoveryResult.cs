namespace SbomQualityGate.Application.Models;

public class ReportDiscoveryResult
{
    public IReadOnlyList<DiscoveredFeature> Features { get; init; } = [];
    public IReadOnlyList<DiscoveredProfile> Profiles { get; init; } = [];
}
