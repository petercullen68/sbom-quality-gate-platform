namespace SbomQualityGate.Domain.Entities;

public class SbomProfile
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public bool IsUserDefined { get; init; }

    public DateTime DiscoveredAt { get; init; }
}
