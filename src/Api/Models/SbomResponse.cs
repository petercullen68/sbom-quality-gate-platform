namespace SbomQualityGate.Api.Models;

public sealed class SbomResponse
{
    public Guid Id { get; init; }

    public Guid ProductId { get; init; }

    public string Version { get; init; } = string.Empty;

    public string SpecType { get; init; } = string.Empty;

    public string SpecVersion { get; init; } = string.Empty;

    public int ComponentCount { get; init; }

    public DateTime UploadedAt { get; init; }
}
