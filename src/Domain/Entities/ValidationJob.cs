namespace SbomQualityGate.Domain.Entities;

using SbomQualityGate.Domain.Enums;

public class ValidationJob
{
    public Guid Id { get; set; }

    public Guid SbomId { get; set; }

    public ValidationJobStatus Status { get; set; }

    public string Profile { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}