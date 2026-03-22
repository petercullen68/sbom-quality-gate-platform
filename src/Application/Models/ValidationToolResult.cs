using SbomQualityGate.Domain.Enums;

namespace SbomQualityGate.Application.Models;

public class ValidationToolResult
{
    public ValidationStatus Status { get; init; }
    public double Score { get; init; }
    public string ReportJson { get; init; } = string.Empty;
}
