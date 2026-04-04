using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Application.Models;
using SbomQualityGate.Domain.Enums;

namespace SbomQualityGate.IntegrationTests.Infrastructure;

/// <summary>
/// Replaces the real sbomqs process in integration tests where sbomqs
/// is not available (e.g. CI). Returns a realistic report structure
/// so downstream handlers (discovery, spec conformance) behave normally.
/// </summary>
public class StubValidationTool : IValidationTool
{
    public static readonly string ReportJson = """
                                               {
                                                 "files": [
                                                   {
                                                     "sbom_quality_score": 85,
                                                     "comprehenssive": [
                                                       { "category": "Identification", "feature": "comp_with_name",    "score": 10, "ignored": false },
                                                       { "category": "Identification", "feature": "comp_with_version", "score": 10, "ignored": false },
                                                       { "category": "Provenance",     "feature": "sbom_authors",      "score": 0,  "ignored": false },
                                                       { "category": "Integrity",      "feature": "sbom_signature",    "score": 0,  "ignored": true  }
                                                     ],
                                                     "profiles": [
                                                       { "profile": "Interlynk", "score": 5.82, "grade": "D", "message": "Interlynk Scoring Profile" }
                                                     ]
                                                   }
                                                 ]
                                               }
                                               """;

    public Task<ValidationToolResult> ValidateAsync(
        string sbomJson,
        string profile,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(new ValidationToolResult
        {
            Status = ValidationStatus.Pass,
            Score = 85,
            ReportJson = ReportJson
        });
    }
}
