---
sidebar_position: 1
---

# API Reference

SBOM Quality Gate exposes a RESTful API for submitting SBOMs and retrieving validation results.

## Base URL

```
https://your-deployment.example.com/api
```

In development:
```
https://localhost:5001/api
```

## Authentication

:::caution Coming Soon
API key authentication is planned but not yet implemented. Currently, the API is unauthenticated.
:::

## Content Types

- **Request**: `application/json` or `multipart/form-data` (file uploads)
- **Response**: `application/json`

## Error Responses

All errors follow the RFC 7807 Problem Details format:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "command.SbomJson must be a valid CycloneDX or SPDX document.",
  "traceId": "00-abc123-def456-00"
}
```

### Common Error Codes

| Status | Description |
|--------|-------------|
| 400 | Bad Request — Invalid input or validation error |
| 404 | Not Found — Resource doesn't exist |
| 413 | Payload Too Large — File exceeds size limit |
| 500 | Internal Server Error — Unexpected failure |

## Endpoints Overview

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/sboms` | Submit SBOM via JSON body |
| `POST` | `/sboms/upload` | Submit SBOM via file upload |
| `GET` | `/sboms/{id}` | Get SBOM metadata |
| `POST` | `/features/discover` | Discover features from report |

## OpenAPI / Swagger

In development mode, interactive API documentation is available:

```
https://localhost:5001/swagger
```

## Rate Limiting

:::caution Coming Soon
Rate limiting is planned but not yet implemented.
:::

## API Sections

- [SBOMs](./sboms) — Submit and retrieve SBOMs
- [Validation Jobs](./validation-jobs) — Job status and results
- [Features](./features) — Feature discovery endpoint
