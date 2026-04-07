---
sidebar_position: 4
---

# Validation Profiles

:::caution Coming Soon
Validation profiles are designed but not yet implemented. This page documents the planned functionality.
:::

## Overview

Validation profiles allow you to define custom pass/fail criteria for different contexts:

- **Team profiles**: Different standards per team
- **Product profiles**: Stricter requirements for critical products
- **Environment profiles**: Relaxed for dev, strict for production

## Planned Features

### Profile Definition

```json
{
  "name": "strict-compliance",
  "description": "For production deployments requiring NIS2 compliance",
  "thresholds": {
    "minimumScore": 90,
    "requiredFeatures": [
      "SBOM-GQ-001",  // Has creation timestamp
      "SBOM-GQ-015",  // Components have PURLs
      "SBOM-GQ-022"   // Licenses declared
    ]
  },
  "specVersions": {
    "CycloneDX": ">=1.4",
    "SPDX": ">=2.3"
  }
}
```

### Profile Hierarchy

Profiles can be scoped at different levels:

```
Organization Default
    └── Team Override
        └── Product Override
```

The most specific matching profile applies.

### Enforcement

Profiles can have different enforcement modes:

| Mode | Behavior |
|------|----------|
| `warn` | Log warning, return Pass |
| `enforce` | Fail if criteria not met |
| `block` | Fail and prevent downstream processing |

### Effective Dates

Profiles can be scheduled to take effect in the future:

```json
{
  "name": "nis2-compliance",
  "enforcementDate": "2025-01-01",
  "gracePeriodDays": 90
}
```

During the grace period, violations produce warnings instead of failures.

## Current Behavior

Until profiles are implemented:

- All SBOMs use the default profile: `NIS2-Default`
- Pass threshold is fixed at 80
- No spec version requirements

## Design Documents

The conformance policy engine design includes:

- `ConformancePolicy` entity
- `PolicyTier` with severity levels (Warning/Error)
- `PolicyRule` for individual checks
- `PolicyEvaluationResult` for storing outcomes

See the architecture documentation for implementation details.

## Next Steps

- [Quality Scoring](./quality-scoring.md) — Understanding the current scoring
- [Architecture Overview](../architecture/overview.md) — System design
