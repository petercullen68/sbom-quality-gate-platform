---
sidebar_position: 2
---

# SBOMs API

Endpoints for submitting and retrieving Software Bills of Materials.

## Submit SBOM (JSON)

Submit an SBOM as a JSON request body.

```http
POST /api/sboms
Content-Type: application/json
```

### Request Body

```json
{
  "team": "platform",
  "project": "my-service",
  "version": "1.2.3",
  "sbomJson": "{\"bomFormat\":\"CycloneDX\",\"specVersion\":\"1.5\",\"components\":[...]}"
}
```

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `team` | string | Yes | Team or organization identifier |
| `project` | string | Yes | Project name |
| `version` | string | Yes | Version or build identifier |
| `sbomJson` | string | Yes | Raw SBOM JSON (CycloneDX or SPDX) |

### Response

**201 Created**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**400 Bad Request**

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "sbomJson": ["command.SbomJson must be a valid CycloneDX or SPDX document."]
  },
  "traceId": "00-abc123-def456-00"
}
```

### Example

```bash
curl -X POST https://localhost:5001/api/sboms \
  -H "Content-Type: application/json" \
  -d '{
    "team": "platform",
    "project": "api-gateway",
    "version": "2.0.0",
    "sbomJson": "{\"bomFormat\":\"CycloneDX\",\"specVersion\":\"1.5\",\"version\":1,\"components\":[]}"
  }'
```

---

## Submit SBOM (File Upload)

Submit an SBOM as a file upload.

```http
POST /api/sboms/upload
Content-Type: multipart/form-data
```

### Form Fields

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `file` | file | Yes | SBOM JSON file |
| `team` | string | Yes | Team or organization identifier |
| `project` | string | Yes | Project name |
| `version` | string | Yes | Version or build identifier |

### Response

**201 Created**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**413 Payload Too Large**

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.13",
  "title": "File too large",
  "status": 413,
  "detail": "Maximum allowed size is 5242880 bytes.",
  "traceId": "00-abc123-def456-00"
}
```

### Example

```bash
curl -X POST https://localhost:5001/api/sboms/upload \
  -F "file=@sbom.json" \
  -F "team=platform" \
  -F "project=api-gateway" \
  -F "version=2.0.0"
```

---

## Get SBOM

Retrieve metadata for a submitted SBOM.

```http
GET /api/sboms/{id}
```

### Path Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | GUID | SBOM identifier |

### Response

**200 OK**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "team": "platform",
  "project": "api-gateway",
  "version": "2.0.0",
  "specType": "CycloneDX",
  "specVersion": "1.5",
  "componentCount": 127,
  "uploadedAt": "2026-04-06T14:30:00Z"
}
```

**404 Not Found**

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found",
  "status": 404,
  "traceId": "00-abc123-def456-00"
}
```

### Example

```bash
curl https://localhost:5001/api/sboms/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

---

## Response Fields

### SBOM Response

| Field | Type | Description |
|-------|------|-------------|
| `id` | GUID | Unique identifier |
| `team` | string | Team that submitted the SBOM |
| `project` | string | Project name |
| `version` | string | Version or build identifier |
| `specType` | string | `CycloneDX` or `SPDX` |
| `specVersion` | string | Spec version (e.g., `1.5`, `SPDX-2.3`) |
| `componentCount` | integer | Number of components in the SBOM |
| `uploadedAt` | datetime | ISO 8601 timestamp |

---

## Side Effects

When an SBOM is submitted:

1. The SBOM is parsed and validated
2. Metadata is extracted (spec type, version, component count)
3. A `ValidationJob` is created with `Pending` status
4. A PostgreSQL `NOTIFY validation_jobs` signal is sent
5. The background worker picks up the job for processing

:::tip
The response returns immediately after the SBOM is persisted. Validation happens asynchronously in the background.
:::
