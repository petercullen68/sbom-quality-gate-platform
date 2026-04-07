---
sidebar_position: 3
---

# Quality Scoring

SBOM Quality Gate uses [sbomqs](https://github.com/interlynk-io/sbomqs) to assess SBOM quality. This page explains how scoring works.

## What is sbomqs?

sbomqs (SBOM Quality Score) is an open-source tool by Interlynk that evaluates SBOMs against a comprehensive set of quality criteria. It produces a score from 0-100 based on:

- Structural compliance
- Metadata completeness
- Component information quality
- Licensing information
- Security-relevant data

## Score Categories

sbomqs organizes checks into categories:

### Structural

Does the SBOM have the expected structure?

| Check | Description |
|-------|-------------|
| Valid format | Parses as valid CycloneDX or SPDX |
| Spec version | Uses a supported specification version |
| Required fields | All mandatory fields present |

### NTIA Minimum Elements

Does the SBOM meet NTIA minimum requirements?

| Check | Description |
|-------|-------------|
| Supplier name | Components have supplier information |
| Component name | All components are named |
| Component version | All components have versions |
| Unique identifiers | Components have unique IDs |
| Dependency relationships | Dependencies are documented |
| Author information | SBOM author is identified |
| Timestamp | SBOM has creation timestamp |

### Quality

How complete is the component information?

| Check | Description |
|-------|-------------|
| Package URLs (PURLs) | Components have PURLs for identification |
| CPE identifiers | Components have CPEs for vulnerability matching |
| License information | Licenses are declared |
| Checksums | File/package hashes provided |
| External references | Links to source, documentation |

### Semantic

Is the data meaningful and consistent?

| Check | Description |
|-------|-------------|
| Version formats | Versions follow semantic versioning |
| License validity | Licenses are SPDX-recognized |
| URL validity | External references are valid URLs |

## Score Calculation

sbomqs calculates scores at multiple levels:

```
Component Score (0-100)
    └── Average of applicable checks

File Score (0-100)
    └── Weighted average of component scores
    
Overall Score (0-100)
    └── Aggregate across all files
```

### Example Output

```json
{
  "files": [
    {
      "file_name": "sbom.json",
      "sbom_quality_score": 85.5,
      "comprehensive": [
        {
          "feature": "SBOM has a creation timestamp",
          "category": "NTIA-minimum-elements",
          "score": 10,
          "max_score": 10
        },
        {
          "feature": "Components have package URLs",
          "category": "Quality",
          "score": 8,
          "max_score": 10
        }
      ]
    }
  ]
}
```

## Pass/Fail Threshold

SBOM Quality Gate uses a configurable threshold (default: 80):

| Score | Result |
|-------|--------|
| ≥ 80 | **Pass** ✅ |
| < 80 | **Fail** ❌ |

:::tip
The threshold will be configurable via validation profiles in a future release.
:::

## Improving Your Score

### Low-Hanging Fruit

These improvements typically have high impact:

1. **Add PURLs**: Package URLs enable vulnerability matching
   ```json
   "purl": "pkg:npm/lodash@4.17.21"
   ```

2. **Include Licenses**: Declare licenses for all components
   ```json
   "licenses": [{ "license": { "id": "MIT" } }]
   ```

3. **Add Supplier Info**: Identify component suppliers
   ```json
   "supplier": { "name": "Lodash Contributors" }
   ```

### Generation Tool Tips

- Use the latest version of your SBOM generation tool
- Enable verbose/complete mode if available
- Generate at build time (not from manifest files alone)
- Include transitive dependencies

### Common Issues

| Issue | Cause | Fix |
|-------|-------|-----|
| Missing PURLs | Tool doesn't generate them | Use a tool that supports PURLs |
| No licenses | Private/internal packages | Add license info to package metadata |
| Missing versions | Dynamic dependencies | Pin dependency versions |
| No relationships | Manifest-only generation | Use build-time generation |

## Feature Discovery

SBOM Quality Gate automatically tracks which sbomqs features exist:

```
sbomqs 0.1.0 → 50 features tracked
sbomqs 0.2.0 → 55 features (5 new discovered)
```

This enables:
- Custom scoring profiles (weight certain features higher)
- Trend analysis (how does your score change over time?)
- Team comparisons (which teams score highest?)

## Next Steps

- [Validation Profiles](./profiles.md) — Custom pass/fail rules
- [Quick Start](../getting-started/quick-start.md) — Submit your first SBOM
- [sbomqs Documentation](https://github.com/interlynk-io/sbomqs) — Full sbomqs reference
