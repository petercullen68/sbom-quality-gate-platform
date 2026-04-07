---
sidebar_position: 1
slug: /
---

# Introduction

**SBOM Quality Gate** is a centralized Software Bill of Materials (SBOM) validation and quality orchestration platform. It serves as a marshalling point where SBOMs are uploaded once and fanned out to multiple downstream analysis tools, with results aggregated into a unified view.

## The Problem

The SBOM ecosystem is fragmented. Organizations adopting SBOMs for compliance (NIS2, FDA, executive orders) face a proliferation of tools:

- Quality scoring tools (sbomqs, sbom-scorecard)
- Vulnerability scanners (Dependency-Track, Grype)
- License compliance checkers
- Format validators

Each tool has its own API, its own output format, and its own way of ingesting SBOMs. This creates integration overhead and scattered results.

## The Solution

SBOM Quality Gate acts as the single point of ingestion. You submit your SBOM once, and the platform:

1. **Validates** the SBOM against schema specifications (CycloneDX, SPDX)
2. **Scores** quality using configurable profiles
3. **Routes** to downstream tools via a pluggable architecture
4. **Aggregates** results into a unified pass/fail gate

```
┌─────────────┐     ┌───────────────────┐     ┌──────────────────┐
│  CI/CD      │────▶│  SBOM Quality     │────▶│  Quality Scoring │
│  Pipeline   │     │  Gate API         │     │  (sbomqs)        │
└─────────────┘     └───────────────────┘     └──────────────────┘
                              │
                              ├────────────────▶ Dependency-Track
                              │
                              └────────────────▶ Future Integrations
```

## Key Features

- **Schema Validation**: Validates SBOMs against CycloneDX 1.4/1.5/1.6 and SPDX 2.3 schemas
- **Quality Scoring**: Integrates with sbomqs for comprehensive quality assessment
- **Validation Profiles**: Define custom pass/fail thresholds per team, product, or environment
- **Async Processing**: Background worker processes validation jobs without blocking uploads
- **Extensible Architecture**: Clean abstractions (`IValidationTool`) for adding new downstream tools
- **API-First**: RESTful API for integration with CI/CD pipelines and other systems

## Quick Links

- [Quick Start Guide](./getting-started/quick-start) — Get up and running in 5 minutes
- [Architecture Overview](./architecture/overview) — Understand the system design
- [API Reference](./api) — Integrate with your pipeline

## License

SBOM Quality Gate is licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).
