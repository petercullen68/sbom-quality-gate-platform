namespace SbomQualityGate.Application.UseCases;

public class SubmitSbomCommand
{
    public string Team { get; set; } = string.Empty;
    public string Project { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string SbomJson { get; set; } = string.Empty;
}