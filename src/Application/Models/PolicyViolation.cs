using SbomQualityGate.Domain.Enums;

namespace SbomQualityGate.Application.Models;

public class PolicyViolation
{
    public string JsonPath { get; init; } = string.Empty;
    public string TierName { get; init; } = string.Empty;
    public PolicyTierSeverity Severity { get; init; }
    public string Description { get; init; } = string.Empty;

    // Null = active now; populated when the tier has a future enforcement date
    public DateTime? EnforcementDate { get; init; }
}
