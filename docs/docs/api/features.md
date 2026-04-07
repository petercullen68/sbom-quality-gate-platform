---
sidebar_position: 4
---

# Features API

The Features API allows discovery of SBOM quality features from validation reports. This keeps the feature catalog current as sbomqs evolves.

## Discover Features

Extract and persist new features from a validation report.

```http
POST /api/features/discover
Content-Type: application/json
```

### Request Body

The request body should be a raw sbomqs report JSON:

```json
{
  "files": [
    {
      "comprehensive": [
        {
          "feature": "SBOM-GQ-001",
          "category": "quality",
          "ignored": false
        },
        {
          "feature": "SBOM-GQ-002",
          "category": "licensing",
          "ignored": true
        }
      ]
    }
  ]
}
```

:::note Spelling Variation
The endpoint supports both `comprehensive` and `comprehenssive` (typo in current sbomqs versions) to ensure compatibility as the upstream tool evolves.
:::

### Response

**200 OK**

```json
{}
```

The endpoint returns an empty success response. New features are persisted silently.

**400 Bad Request**

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "report": ["Missing or invalid 'files' array."]
  },
  "traceId": "00-abc123-def456-00"
}
```

### Example

```bash
# Extract features from a saved sbomqs report
curl -X POST https://localhost:5001/api/features/discover \
  -H "Content-Type: application/json" \
  -d @sbomqs-report.json
```

---

## Behavior

### Deduplication

- Features are identified by their `feature` field (e.g., `SBOM-GQ-001`)
- Existing features are not re-added
- Comparison is case-insensitive

### Persisted Fields

| Field | Source | Description |
|-------|--------|-------------|
| `Feature` | `feature` | Unique feature identifier |
| `Category` | `category` | Feature category (quality, licensing, etc.) |
| `Ignored` | `ignored` | Whether the feature is ignored by default |
| `DiscoveredAt` | Generated | Timestamp of first discovery |

### Automatic Discovery

This endpoint is called internally by the worker after each validation completes. You typically don't need to call it manually unless:

- You're bulk-loading features from historical reports
- You're testing feature extraction logic

---

## Use Cases

### Feature Catalog Maintenance

As sbomqs adds new quality checks, they're automatically discovered:

```
sbomqs 0.1.0 → 50 features
sbomqs 0.2.0 → 55 features (5 new discovered automatically)
sbomqs 0.3.0 → 60 features (5 more discovered)
```

### Custom Scoring Profiles

The feature catalog enables future functionality:

- Define custom scoring weights per feature
- Ignore specific features for certain teams
- Track feature coverage over time

---

## Database Schema

Features are stored in the `SbomFeatures` table:

```sql
CREATE TABLE "SbomFeatures" (
    "Id" uuid PRIMARY KEY,
    "Feature" text NOT NULL UNIQUE,
    "Category" text NOT NULL,
    "Ignored" boolean NOT NULL,
    "DiscoveredAt" timestamp NOT NULL
);
```
