---
sidebar_position: 1
---

# SBOM Overview

A **Software Bill of Materials (SBOM)** is a formal, machine-readable inventory of software components and dependencies. Think of it as an ingredient list for software.

## Why SBOMs Matter

### Supply Chain Security

Modern applications are built from hundreds of open-source and third-party components. When a vulnerability is discovered (like Log4Shell), organizations need to quickly answer: "Are we affected?"

Without an SBOM: Manual investigation, delayed response, missed instances.

With an SBOM: Automated search, immediate answers, comprehensive coverage.

### Regulatory Compliance

SBOMs are increasingly required by regulation:

| Regulation | Region | Requirement |
|------------|--------|-------------|
| **Executive Order 14028** | US | Federal software suppliers must provide SBOMs |
| **NIS2 Directive** | EU | Supply chain security requirements include SBOMs |
| **FDA Cybersecurity Guidance** | US | Medical device manufacturers must provide SBOMs |
| **CISA SBOM Requirements** | US | Critical infrastructure software |

### Software Transparency

SBOMs enable:
- License compliance verification
- Component age and maintenance tracking
- Dependency risk assessment
- Procurement decisions

## SBOM Formats

Two formats dominate the ecosystem:

### CycloneDX

Developed by OWASP, designed specifically for security use cases.

```json
{
  "bomFormat": "CycloneDX",
  "specVersion": "1.5",
  "version": 1,
  "metadata": {
    "timestamp": "2026-04-06T12:00:00Z",
    "component": {
      "name": "my-application",
      "version": "1.0.0"
    }
  },
  "components": [
    {
      "type": "library",
      "name": "lodash",
      "version": "4.17.21",
      "purl": "pkg:npm/lodash@4.17.21"
    }
  ]
}
```

**Strengths:**
- Security-focused (vulnerabilities, advisories)
- Supports VEX (Vulnerability Exploitability eXchange)
- Active development, frequent releases
- Good tooling ecosystem

### SPDX

Developed by the Linux Foundation, originally for license compliance.

```json
{
  "spdxVersion": "SPDX-2.3",
  "dataLicense": "CC0-1.0",
  "SPDXID": "SPDXRef-DOCUMENT",
  "name": "my-application",
  "packages": [
    {
      "SPDXID": "SPDXRef-Package-lodash",
      "name": "lodash",
      "versionInfo": "4.17.21",
      "downloadLocation": "https://registry.npmjs.org/lodash/-/lodash-4.17.21.tgz"
    }
  ]
}
```

**Strengths:**
- ISO/IEC 5962:2021 standard
- Strong license compliance features
- Relationship modeling
- Mature specification

## SBOM Quality

Not all SBOMs are created equal. A high-quality SBOM includes:

| Attribute | Description |
|-----------|-------------|
| **Completeness** | All components are listed |
| **Accuracy** | Versions and identifiers are correct |
| **Timeliness** | Generated close to build time |
| **Depth** | Transitive dependencies included |
| **Identifiers** | PURLs, CPEs for vulnerability matching |
| **Licensing** | License information for each component |

SBOM Quality Gate uses [sbomqs](https://github.com/interlynk-io/sbomqs) to assess these attributes and produce a quality score.

## Generating SBOMs

Common tools for generating SBOMs:

| Ecosystem | Tool | Output Format |
|-----------|------|---------------|
| .NET | `CycloneDX` dotnet tool | CycloneDX |
| .NET | `sbom-tool` (Microsoft) | SPDX |
| Node.js | `@cyclonedx/cyclonedx-npm` | CycloneDX |
| Python | `cyclonedx-py` | CycloneDX |
| Java | `cyclonedx-maven-plugin` | CycloneDX |
| Container | `syft` | Both |
| Universal | `trivy` | Both |

Example for .NET:

```bash
# Install the tool
dotnet tool install --global CycloneDX

# Generate SBOM
dotnet CycloneDX ./MyProject.csproj -o sbom.json -j
```

## Next Steps

- [Validation Workflow](./validation-workflow) — How SBOMs are processed
- [Quality Scoring](./quality-scoring) — Understanding sbomqs scores
- [Quick Start](../getting-started/quick-start) — Submit your first SBOM
