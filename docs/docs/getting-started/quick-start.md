---
sidebar_position: 2
---

# Quick Start

This guide walks you through submitting your first SBOM and viewing the validation results.

## Submit an SBOM

### Using curl

```bash
# Upload a CycloneDX SBOM file
curl -X POST https://localhost:5001/api/sboms/upload \
  -F "file=@my-sbom.json" \
  -F "team=platform" \
  -F "project=my-app" \
  -F "version=1.0.0"
```

### Using the JSON endpoint

```bash
# Submit SBOM JSON directly
curl -X POST https://localhost:5001/api/sboms \
  -H "Content-Type: application/json" \
  -d '{
    "team": "platform",
    "project": "my-app",
    "version": "1.0.0",
    "sbomJson": "{\"bomFormat\":\"CycloneDX\",\"specVersion\":\"1.5\",\"components\":[]}"
  }'
```

Both endpoints return the created SBOM ID:

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

## Check Validation Status

A validation job is automatically created when you submit an SBOM. The background worker picks it up and processes it asynchronously.

```bash
# Get SBOM details (includes validation status once complete)
curl https://localhost:5001/api/sboms/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

Response:

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "team": "platform",
  "project": "my-app",
  "version": "1.0.0",
  "specType": "CycloneDX",
  "specVersion": "1.5",
  "componentCount": 127,
  "uploadedAt": "2026-04-06T12:00:00Z"
}
```

## Understanding Results

### Validation Status

| Status | Meaning |
|--------|---------|
| `Pass` | SBOM meets the quality threshold (score ≥ 80) |
| `Fail` | SBOM quality is below threshold |

### Job Status

| Status | Meaning |
|--------|---------|
| `Pending` | Job queued, waiting for worker |
| `InProgress` | Worker is processing the job |
| `Completed` | Validation finished successfully |
| `Failed` | Job failed (infrastructure error, not quality failure) |

:::tip Pass vs Completed
A `Completed` job status means the validation ran successfully. The SBOM can still have a `Fail` validation status if its quality score is below threshold. `Failed` job status indicates an infrastructure error (e.g., sbomqs crashed, database error).
:::

## Viewing the Swagger UI

In development mode, the API includes Swagger documentation:

```
https://localhost:5001/swagger
```

This provides an interactive way to explore and test all API endpoints.

## Example: CI/CD Integration

Here's how you might integrate SBOM Quality Gate into a GitHub Actions workflow:

```yaml
- name: Generate SBOM
  run: |
    dotnet tool install --global CycloneDX
    dotnet CycloneDX ./MyProject.csproj -o sbom.json -j

- name: Submit to Quality Gate
  run: |
    RESPONSE=$(curl -s -X POST ${{ vars.SBOM_GATE_URL }}/api/sboms/upload \
      -F "file=@sbom.json" \
      -F "team=${{ github.repository_owner }}" \
      -F "project=${{ github.event.repository.name }}" \
      -F "version=${{ github.sha }}")
    
    SBOM_ID=$(echo $RESPONSE | jq -r '.id')
    echo "SBOM_ID=$SBOM_ID" >> $GITHUB_ENV

- name: Wait for Validation
  run: |
    # Poll for completion (simplified - use proper polling in production)
    sleep 30
    curl -s ${{ vars.SBOM_GATE_URL }}/api/sboms/${{ env.SBOM_ID }}
```

## Next Steps

- [Configuration](./configuration) — Customize validation profiles and thresholds
- [API Reference](../api) — Full API documentation
- [CI/CD Integration](../integrations/ci-cd) — Detailed pipeline integration guide
