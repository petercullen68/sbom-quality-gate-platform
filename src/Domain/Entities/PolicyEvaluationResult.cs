namespace SbomQualityGate.Domain.Entities;

public class PolicyEvaluationResult
{
    public Guid Id { get; init; }

    public Guid ValidationResultId { get; init; }

    // Null when no policy was applicable at evaluation time
    public Guid? PolicyId { get; init; }

    // Snapshot of policy name at evaluation time — policy name may change later
    public string PolicyName { get; init; } = string.Empty;

    public DateTime EvaluatedAt { get; init; }

    // Structured violations as jsonb — see PolicyViolation value object
    public string ViolationsJson { get; init; } = string.Empty;

    // Navigation
    public ValidationResult ValidationResult { get; init; } = null!;
}
