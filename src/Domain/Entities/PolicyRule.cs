namespace SbomQualityGate.Domain.Entities;

public class PolicyRule
{
    public Guid Id { get; init; }

    public Guid PolicyId { get; init; }

    public Guid TierId { get; init; }

    // JSON path expression e.g. $.components[*].licenses
    public string JsonPath { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    // Navigation
    public ConformancePolicy Policy { get; init; } = null!;
    public PolicyTier Tier { get; init; } = null!;
}
