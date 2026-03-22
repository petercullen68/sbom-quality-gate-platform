namespace SbomQualityGate.Api.Configuration;

public sealed class UploadOptions
{
    public const string SectionName = "Upload";

    // Default to 5 MB if config is missing.
    public long MaxUploadBytes { get; init; } = 5 * 1024 * 1024;
}
