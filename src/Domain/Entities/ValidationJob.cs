using SbomQualityGate.Domain.Enums;

namespace SbomQualityGate.Domain.Entities;

public class ValidationJob
{
    public Guid Id { get; init; }

    public Guid SbomId { get; init; }

    public ValidationJobStatus Status { get; set; }

    public string Profile { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    public DateTime? StartedAt { get; init; }

    public DateTime? CompletedAt { get; set; }
    
    public int RetryCount { get; set; }
    
    public string? FailureReason { get; set; }
}
