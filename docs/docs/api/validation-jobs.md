---
sidebar_position: 3
---

# Validation Jobs API

:::caution Coming Soon
Direct API access to validation jobs is planned for a future release. Currently, jobs are created automatically when SBOMs are submitted and processed by the background worker.
:::

## Planned Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/jobs` | List validation jobs |
| `GET` | `/jobs/{id}` | Get job details and result |
| `GET` | `/sboms/{sbomId}/jobs` | List jobs for an SBOM |

## Current Behavior

When you submit an SBOM via `POST /api/sboms` or `POST /api/sboms/upload`:

1. A validation job is automatically created
2. The job uses the default profile (`NIS2-Default`)
3. The background worker processes the job
4. Results are stored in the database

## Job Lifecycle

```
┌─────────┐     ┌────────────┐     ┌───────────┐     ┌─────────┐
│ Pending │────▶│ InProgress │────▶│ Completed │     │  Failed │
└─────────┘     └────────────┘     └───────────┘     └─────────┘
                      │                   ▲                ▲
                      │                   │                │
                      └───────────────────┴────────────────┘
                              (retry up to 3x)
```

## Future: Custom Profiles

The planned API will support specifying validation profiles:

```http
POST /api/sboms
Content-Type: application/json

{
  "team": "platform",
  "project": "my-service",
  "version": "1.0.0",
  "sbomJson": "...",
  "profile": "strict-compliance"  // Custom profile
}
```

## Polling for Results

Until the jobs API is available, you can check SBOM status by polling:

```bash
# Submit SBOM
SBOM_ID=$(curl -s -X POST https://localhost:5001/api/sboms/upload \
  -F "file=@sbom.json" \
  -F "team=platform" \
  -F "project=my-app" \
  -F "version=1.0.0" | jq -r '.id')

# Poll for completion (simplified)
for i in {1..30}; do
  sleep 2
  STATUS=$(curl -s https://localhost:5001/api/sboms/$SBOM_ID)
  echo "Checking... $STATUS"
done
```

A proper jobs endpoint with status will be available soon.
