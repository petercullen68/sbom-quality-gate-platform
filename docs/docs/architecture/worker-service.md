---
sidebar_position: 3
---

# Worker Service

The Worker service is a long-running background process that consumes validation jobs from the queue and processes them using configured validation tools.

## Architecture

```
┌───────────────────────────────────────────────────────────────────┐
│                        Worker Service                              │
│                                                                    │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │                    Worker (BackgroundService)                │  │
│  │                                                              │  │
│  │   ┌──────────────────────┐    ┌───────────────────────────┐ │  │
│  │   │ PostgresNotification │    │     JobProcessor          │ │  │
│  │   │ Listener             │───▶│                           │ │  │
│  │   │ (LISTEN/NOTIFY)      │    │  ┌─────────────────────┐  │ │  │
│  │   └──────────────────────┘    │  │ ProcessNextValidation│  │ │  │
│  │                               │  │ JobHandler           │  │ │  │
│  │   Fallback: 30s polling ────▶ │  └─────────────────────┘  │ │  │
│  │                               └───────────────────────────┘ │  │
│  └─────────────────────────────────────────────────────────────┘  │
│                                                                    │
│                          ▼                                         │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │                  SbomQsValidationTool                        │  │
│  │  ┌────────────────┐  ┌────────────────┐  ┌───────────────┐  │  │
│  │  │ Circuit Breaker│  │ Process Runner │  │ sbomqs CLI    │  │  │
│  │  └────────────────┘  └────────────────┘  └───────────────┘  │  │
│  └─────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────┘
```

## Job Lifecycle

### 1. Notification Arrival

When a new SBOM is submitted, the API commits the transaction and sends:

```sql
NOTIFY validation_jobs
```

The `PostgresNotificationListener` maintains a persistent connection listening for this channel:

```csharp
await using var cmd = new NpgsqlCommand("LISTEN validation_jobs;", _connection);
await cmd.ExecuteNonQueryAsync(cancellationToken);
```

### 2. Job Claim

The worker uses `SELECT FOR UPDATE SKIP LOCKED` to atomically claim the next pending job:

```sql
UPDATE "ValidationJobs"
SET "Status" = 1,  -- InProgress
    "StartedAt" = NOW()
WHERE "Id" = (
    SELECT "Id"
    FROM "ValidationJobs"
    WHERE "Status" = 0  -- Pending
    ORDER BY "CreatedAt"
    FOR UPDATE SKIP LOCKED
    LIMIT 1
)
RETURNING *;
```

This pattern:
- Prevents multiple workers from claiming the same job
- Skips jobs locked by other workers (no blocking)
- Orders by creation time (FIFO)

### 3. Validation

The handler loads the SBOM and invokes the validation tool:

```csharp
var sbom = await sbomRepository.GetByIdAsync(job.SbomId, cancellationToken);
var result = await validationTool.ValidateAsync(
    sbom.SbomJson,
    job.Profile,
    cancellationToken);
```

### 4. Completion

On success, the job is marked complete and the result is stored:

```csharp
var result = new ValidationResult
{
    Id = Guid.NewGuid(),
    ValidationJobId = job.Id,
    Status = resultData.Status,
    Score = resultData.Score,
    ReportJson = resultData.ReportJson,
    Profile = job.Profile,
    CreatedAt = DateTime.UtcNow
};

await jobRepository.CompleteJobAsync(job, result, cancellationToken);
```

### 5. Error Handling

If validation fails, the job is retried up to 3 times:

```csharp
job.RetryCount++;

if (job.RetryCount >= 3)
{
    job.Status = ValidationJobStatus.Failed;
    job.CompletedAt = DateTime.UtcNow;
}
else
{
    job.Status = ValidationJobStatus.Pending; // Will be retried
}

job.FailureReason = reason;
```

## Circuit Breaker

The `SbomQsValidationTool` implements a circuit breaker to handle sbomqs failures gracefully:

```csharp
private int _failureCount;
private DateTime _blockedUntil = DateTime.MinValue;

if (DateTime.UtcNow < _blockedUntil)
{
    throw new InvalidOperationException("sbomqs temporarily unavailable (circuit open)");
}

try
{
    // ... validation logic ...
    _failureCount = 0; // Reset on success
}
catch
{
    _failureCount++;
    if (_failureCount >= 5)
    {
        _blockedUntil = DateTime.UtcNow.AddMinutes(1);
        _failureCount = 0;
    }
    throw;
}
```

**Behavior:**
- After 5 consecutive failures, the circuit opens for 1 minute
- During this time, all calls fail immediately (fast fail)
- After 1 minute, the circuit half-opens and allows one attempt
- Success resets the circuit; failure reopens it

## Fallback Polling

If the PostgreSQL notification is missed (connection blip, etc.), fallback polling ensures jobs are still processed:

```csharp
var delayTask = Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

var completed = await Task.WhenAny(dbWaitTask, channelWaitTask, delayTask);

if (completed == delayTask)
{
    FallbackTriggered(logger, null);
}

// Process jobs regardless of trigger source
await processor.ProcessAsync(stoppingToken);
```

## Connection Recovery

The worker handles database disconnections gracefully:

```csharp
catch (Exception ex)
{
    WorkerError(logger, ex);

    // Tear down broken connection
    await listener.StopAsync();

    // Backoff before retry
    await Task.Delay(backoff, stoppingToken);

    // Recreate listener (new connection + LISTEN)
    await listener.StartAsync(stoppingToken);

    // Exponential backoff (max 30s)
    backoff = TimeSpan.FromSeconds(
        Math.Min(backoff.TotalSeconds * 2, 30));
}
```

## Scaling Workers

Multiple worker instances can run concurrently. The `SKIP LOCKED` clause ensures each job is processed by exactly one worker.

For Kubernetes deployments:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: sbomqg-worker
spec:
  replicas: 3  # Scale horizontally
  template:
    spec:
      containers:
        - name: worker
          image: sbomqualitygate-worker
          resources:
            limits:
              cpu: "500m"
              memory: "256Mi"
```

## Monitoring

### Log Events

| EventId | Level | Description |
|---------|-------|-------------|
| 1 | Info | Worker started |
| 2 | Info | Notification received |
| 3 | Info | Fallback polling triggered |
| 4 | Info | Worker shutting down |
| 5 | Error | Error in worker loop |

### Health Indicators

The worker exposes health via job processing:
- Jobs staying `Pending` too long indicates worker issues
- High `FailureReason` rate indicates tool problems
- `RetryCount` approaching 3 indicates persistent failures

## Next Steps

- [Extensibility](./extensibility) — Adding new validation tools
- [API Reference](../api) — How jobs are created
