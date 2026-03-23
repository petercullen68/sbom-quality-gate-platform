namespace SbomQualityGate.Api.Configuration;

public sealed class UploadOptions
{
    public const string SectionName = "Upload";
    
    public long MaxUploadBytes { get; init; } = 5 * 1024 * 1024;
}
