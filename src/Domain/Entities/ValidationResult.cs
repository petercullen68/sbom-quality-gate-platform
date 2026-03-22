using SbomQualityGate.Domain.Enums;

namespace SbomQualityGate.Domain.Entities;

public class ValidationResult
{
    public Guid Id { get; init; }
    public Guid ValidationJobId { get; init; }
    public ValidationStatus Status { get; init; }
    public ValidationJob ValidationJob { get; init; } = null!;

    public double Score { get; init; }

    public string Profile { get; init; } = string.Empty;
    public string ReportJson { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}