namespace Domain.Entities;

public class Sbom
{
    public Guid Id { get; set; }

    public string Team { get; set; } = string.Empty;

    public string Project { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string SpecType { get; set; } = string.Empty; // CycloneDX / SPDX

    public string SpecVersion { get; set; } = string.Empty;

    public string SbomJson { get; set; } = string.Empty; // raw JSON (JSONB later)

    public int ComponentCount { get; set; }

    public bool HasSupplier { get; set; }

    public bool HasLicenses { get; set; }

    public DateTime UploadedAt { get; set; }

    // Navigation (domain-level, not EF-specific)
    public List<ValidationResult> Validations { get; set; } = new();
}