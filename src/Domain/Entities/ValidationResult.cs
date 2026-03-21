using Domain.Enums;

namespace Domain.Entities;

public class ValidationResult
{
    public Guid Id { get; set; }

    public Guid SbomId { get; set; }

    public ValidationStatus Status { get; set; }

    public double Score { get; set; }

    public string Profile { get; set; } = string.Empty;

    public string ReportJson { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}