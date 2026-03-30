namespace SbomQualityGate.Application.Models;

public class DiscoveredFeature
{
    public string Feature { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public bool Ignored { get; init; }
}