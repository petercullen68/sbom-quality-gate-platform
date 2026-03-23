namespace SbomQualityGate.Domain.Entities;

public class SbomFeature
{
    public Guid Id { get; set; }

    public string Category { get; set; } = string.Empty;
    public string Feature { get; set; } = string.Empty;

    public bool Ignored { get; set; }

    public DateTime DiscoveredAt { get; set; }
}
