using System.ComponentModel.DataAnnotations;

namespace SbomQualityGate.Application.UseCases;

public class SubmitSbomCommand
{
    [Required]
    public string Team { get; set; } = string.Empty;

    [Required]
    public string Project { get; set; } = string.Empty;

    [Required]
    public string Version { get; set; } = string.Empty;

    [Required]
    public string SbomJson { get; set; } = string.Empty;
}
