---
sidebar_position: 2
---

# Dependency-Track Integration

:::caution Coming Soon
Dependency-Track integration is planned but not yet implemented. This page documents the intended design.
:::

## Overview

[Dependency-Track](https://dependencytrack.org/) is an intelligent Component Analysis platform that identifies risk in the software supply chain. It excels at:

- Vulnerability identification (CVEs)
- Outdated component detection
- License compliance analysis
- Policy evaluation

SBOM Quality Gate will integrate with Dependency-Track to provide vulnerability context alongside quality scoring.

## Planned Architecture

```
┌──────────────────┐     ┌───────────────────┐     ┌───────────────────┐
│  SBOM Quality    │────▶│  Dependency-Track │────▶│  Vulnerability    │
│  Gate            │     │  API              │     │  Results          │
└──────────────────┘     └───────────────────┘     └───────────────────┘
        │                                                   │
        │                                                   │
        └───────────────────────────────────────────────────┘
                    Aggregated Quality + Vuln Results
```

## Design Considerations

### Async Nature

Unlike sbomqs (synchronous, completes in seconds), Dependency-Track analysis is:

- **Asynchronous**: Submit SBOM, poll for results
- **Slower**: Analysis may take minutes
- **Stateful**: Projects persist in D-Track

This may warrant a separate abstraction:

```csharp
public interface IDependencyAnalysisTool
{
    Task<Guid> SubmitAsync(string sbomJson, CancellationToken ct);
    Task<AnalysisStatus> GetStatusAsync(Guid analysisId, CancellationToken ct);
    Task<AnalysisResult> GetResultAsync(Guid analysisId, CancellationToken ct);
}
```

### Project Mapping

How SBOM Quality Gate concepts map to D-Track:

| Quality Gate | Dependency-Track |
|--------------|------------------|
| Team | Project Group |
| Project | Project |
| Version | Project Version |
| SBOM | BOM |

### Pass/Fail Impact

Open questions:

- Should vulnerability findings affect the pass/fail gate?
- What severity threshold triggers failure?
- How do we handle false positives and suppressions?

## Configuration (Planned)

```json
{
  "DependencyTrack": {
    "Enabled": true,
    "BaseUrl": "https://dtrack.example.com",
    "ApiKey": "...",
    "FailOnCritical": true,
    "FailOnHigh": false,
    "ProjectAutoCreate": true
  }
}
```

## Workflow (Planned)

1. SBOM submitted to Quality Gate
2. Quality Gate runs sbomqs (immediate)
3. Quality Gate submits SBOM to Dependency-Track
4. Background job polls D-Track for completion
5. Results aggregated into unified view

## Alternative: Direct D-Track Integration

For teams already using Dependency-Track, direct integration may be preferred:

```yaml
# GitHub Actions example
- name: Submit to Dependency-Track
  uses: DependencyTrack/gh-upload-sbom@v1
  with:
    serverhostname: 'dtrack.example.com'
    apikey: ${{ secrets.DTRACK_API_KEY }}
    project: 'my-project'
    version: ${{ github.sha }}
    bomfilename: 'sbom.json'
```

Quality Gate would then focus on quality scoring, leaving vulnerability analysis to D-Track directly.

## Next Steps

- [CI/CD Integration](./ci-cd) — Current integration patterns
- [Extensibility](../architecture/extensibility) — Adding new tools
