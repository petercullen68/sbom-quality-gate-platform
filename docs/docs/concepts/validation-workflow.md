---
sidebar_position: 2
---

# Validation Workflow

This page explains how an SBOM flows through the system from upload to validated result.

## End-to-End Flow

```
┌──────────────────────────────────────────────────────────────────────────┐
│                            SBOM Submission                               │
│                                                                          │
│   Client ──POST /api/sboms──▶ API ──parse & validate──▶ Database         │
│                                     │                                    │
│                                     ├── Create Sbom entity               │
│                                     ├── Create ValidationJob (Pending)   │
│                                     └── NOTIFY validation_jobs           │
│                                                                          │
│   Response: 201 Created { "id": "..." }                                  │
└──────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌──────────────────────────────────────────────────────────────────────────┐
│                            Job Processing                                │
│                                                                          │
│   Worker ◀──LISTEN──▶ PostgreSQL                                         │
│      │                                                                   │
│      ├── Claim job (SELECT FOR UPDATE SKIP LOCKED)                       │
│      ├── Load SBOM from database                                         │
│      ├── Write SBOM to temp file                                         │
│      ├── Execute: sbomqs score /tmp/sbom.json --json                     │
│      ├── Parse result, extract score                                     │
│      ├── Create ValidationResult                                         │
│      └── Mark job Completed                                              │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌──────────────────────────────────────────────────────────────────────────┐
│                            Result Available                              │
│                                                                          │
│   ValidationResult:                                                      │
│     - Status: Pass (score >= 80) or Fail (score < 80)                    │
│     - Score: 85.5                                                        │
│     - ReportJson: { full sbomqs output }                                 │
│                                                                          │
└──────────────────────────────────────────────────────────────────────────┘
```

## Step-by-Step Breakdown

### 1. SBOM Submission

When a client submits an SBOM:

```http
POST /api/sboms
Content-Type: application/json

{
  "team": "platform",
  "project": "api-gateway",
  "version": "2.0.0",
  "sbomJson": "{ ... CycloneDX or SPDX document ... }"
}
```

The `SubmitSbomHandler` performs:

1. **JSON Parsing**: Validates the SBOM is valid JSON
2. **Format Detection**: Identifies CycloneDX vs SPDX
3. **Metadata Extraction**: Pulls spec version, component count
4. **Persistence**: Stores the `Sbom` entity
5. **Job Creation**: Creates a `ValidationJob` with `Pending` status
6. **Notification**: Sends `NOTIFY validation_jobs` to wake the worker

The response returns immediately — validation happens in the background.

### 2. Job Claim

The worker service maintains a PostgreSQL connection listening for notifications:

```sql
LISTEN validation_jobs;
```

When notified (or on 30-second fallback polling), the worker claims the next job:

```sql
UPDATE "ValidationJobs"
SET "Status" = 1,  -- InProgress
    "StartedAt" = NOW()
WHERE "Id" = (
    SELECT "Id" FROM "ValidationJobs"
    WHERE "Status" = 0  -- Pending
    ORDER BY "CreatedAt"
    FOR UPDATE SKIP LOCKED
    LIMIT 1
)
RETURNING *;
```

Key behaviors:
- **FIFO**: Jobs are processed in creation order
- **Exclusive**: `FOR UPDATE` locks the row
- **Non-blocking**: `SKIP LOCKED` skips jobs being processed by other workers

### 3. Validation Execution

The `SbomQsValidationTool` executes the sbomqs CLI:

```csharp
// Write SBOM to temp file (sbomqs reads from file, not stdin)
var tempFile = Path.GetTempFileName();
await File.WriteAllTextAsync(tempFile, sbomJson);

// Execute sbomqs
var (exitCode, output, error) = await processRunner.RunAsync(
    "sbomqs",
    $"score {tempFile} --json",
    cancellationToken);
```

The tool:
- Writes the SBOM to a temporary file
- Invokes `sbomqs score <file> --json`
- Parses the JSON output
- Extracts the quality score
- Cleans up the temp file

### 4. Result Creation

Based on the sbomqs output:

```csharp
var score = doc.RootElement
    .GetProperty("files")[0]
    .GetProperty("sbom_quality_score")
    .GetDouble();

var status = score >= 80 
    ? ValidationStatus.Pass 
    : ValidationStatus.Fail;

var result = new ValidationResult
{
    Id = Guid.NewGuid(),
    ValidationJobId = job.Id,
    Status = status,
    Score = score,
    ReportJson = output,  // Full sbomqs output
    Profile = job.Profile,
    CreatedAt = DateTime.UtcNow
};
```

### 5. Job Completion

The job is marked complete and the result is persisted atomically:

```csharp
context.ValidationResults.Add(result);
job.Status = ValidationJobStatus.Completed;
job.CompletedAt = DateTime.UtcNow;
await context.SaveChangesAsync();
```

## Error Handling

### Transient Failures

If validation fails (sbomqs crash, timeout, etc.):

1. Job `RetryCount` is incremented
2. If `RetryCount < 3`: Job returns to `Pending` for retry
3. If `RetryCount >= 3`: Job moves to `Failed` with `FailureReason`

### Circuit Breaker

If sbomqs fails 5 times consecutively:

1. Circuit opens for 1 minute
2. All validation calls fail immediately (fast fail)
3. After 1 minute, one call is allowed through
4. Success resets the circuit; failure reopens it

This prevents cascading failures if sbomqs is down.

## Timing Expectations

| Phase | Typical Duration |
|-------|------------------|
| API request handling | < 100ms |
| Job claim | < 10ms |
| sbomqs execution | 1-5 seconds (varies by SBOM size) |
| Result persistence | < 50ms |

Total time from submission to result: **2-10 seconds** under normal load.

## Next Steps

- [Quality Scoring](./quality-scoring.md) — Understanding the score
- [Worker Service](../architecture/worker-service.md) — Deep dive into job processing
