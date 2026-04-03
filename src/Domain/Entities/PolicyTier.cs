using SbomQualityGate.Domain.Enums;

namespace SbomQualityGate.Domain.Entities;

public class PolicyTier
{
    public Guid Id { get; init; }

    public Guid PolicyId { get; init; }

    public string Name { get; init; } = string.Empty;

    public PolicyTierSeverity Severity { get; init; }

    // Null = active immediately; future date = informational until then
    public DateTime? EnforcementDate { get; init; }

    public int DisplayOrder { get; init; }

    // Navigation
    public ConformancePolicy Policy { get; init; } = null!;

    public ICollection<PolicyRule> Rules { get; init; } = [];
}
