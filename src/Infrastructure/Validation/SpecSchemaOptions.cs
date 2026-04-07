namespace SbomQualityGate.Infrastructure.Validation;

public sealed class SpecSchemaOptions
{
    public const string SectionName = "SpecSchemas";

    public Dictionary<string, string> CycloneDx { get; init; } = [];
    public Dictionary<string, string> Spdx { get; init; } = [];
}
