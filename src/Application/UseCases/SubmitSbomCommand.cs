using System.ComponentModel.DataAnnotations;

namespace SbomQualityGate.Application.UseCases;

public class SubmitSbomCommand
{
    [Required]
    public Guid ProductId { get; init; }

    [Required]
    public string Version { get; init; } = string.Empty;

    [Required]
    public string SbomJson { get; init; } = string.Empty;
}
