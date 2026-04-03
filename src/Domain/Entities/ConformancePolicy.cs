namespace SbomQualityGate.Domain.Entities;

public class ConformancePolicy
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    // Format this policy applies to: "CycloneDX" or "SPDX"
    public string SpecType { get; init; } = string.Empty;

    // Minimum spec version this policy applies to e.g. "1.4", "2.2"
    // Compared as a parsed version at evaluation time
    public string MinSpecVersion { get; init; } = string.Empty;

    public bool IsEnabled { get; set; }

    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; set; }

    // Scope — at most one set; both null = org-level
    public Guid? TeamId { get; init; }
    public Guid? ProductId { get; init; }

    // Navigation
    public Team? Team { get; init; }
    public Product? Product { get; init; }

    public ICollection<PolicyTier> Tiers { get; init; } = [];
    public ICollection<PolicyRule> Rules { get; init; } = [];
}
