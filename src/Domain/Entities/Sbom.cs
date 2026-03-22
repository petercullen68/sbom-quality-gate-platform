namespace SbomQualityGate.Domain.Entities;

public class Sbom
{
    public Guid Id { get; init; }

    public string Team { get; init; } = string.Empty;

    public string Project { get; init; } = string.Empty;

    public string Version { get; init; } = string.Empty;

    public string SpecType { get; init; } = string.Empty; 

    public string SpecVersion { get; init; } = string.Empty;

    public string SbomJson { get; init; } = string.Empty; 

    public int ComponentCount { get; init; }
    
    public DateTime UploadedAt { get; init; }
}