namespace SbomQualityGate.Application.Models;

public class SpecConformanceResult
{
    public bool IsConformant { get; init; }
    public IReadOnlyList<string> Violations { get; init; } = [];
    public IReadOnlyList<string> DeprecationWarnings { get; init; } = [];
    public string SchemaUrl { get; init; } = string.Empty;
    public DateTime FetchedAt { get; init; }
}
