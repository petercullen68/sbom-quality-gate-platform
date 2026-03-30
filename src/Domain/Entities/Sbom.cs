namespace SbomQualityGate.Domain.Entities;

public class Sbom
{
    public Guid Id { get; init; }

    public Guid ProductId { get; init; }

    public string Version { get; init; } = string.Empty;

    public string SpecType { get; init; } = string.Empty;

    public string SpecVersion { get; init; } = string.Empty;

    public string SbomJson { get; init; } = string.Empty;

    public int ComponentCount { get; init; }

    public DateTime UploadedAt { get; init; }

    public Product Product { get; init; } = null!;
}
