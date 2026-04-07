---
sidebar_position: 2
---

# Domain Model

The domain layer contains pure entities that represent the core business concepts. These classes have no external dependencies and focus solely on data structure.

## Entity Relationship Diagram

```
┌─────────────────┐       ┌─────────────────────┐       ┌─────────────────────┐
│      Sbom       │       │   ValidationJob     │       │  ValidationResult   │
├─────────────────┤       ├─────────────────────┤       ├─────────────────────┤
│ Id (PK)         │◄──────│ SbomId (FK)         │◄──────│ ValidationJobId(FK) │
│ Team            │       │ Id (PK)             │       │ Id (PK)             │
│ Project         │       │ Status              │       │ Status              │
│ Version         │       │ Profile             │       │ Score               │
│ SpecType        │       │ CreatedAt           │       │ Profile             │
│ SpecVersion     │       │ StartedAt           │       │ ReportJson          │
│ SbomJson        │       │ CompletedAt         │       │ CreatedAt           │
│ ComponentCount  │       │ RetryCount          │       └─────────────────────┘
│ UploadedAt      │       │ FailureReason       │
└─────────────────┘       └─────────────────────┘

┌─────────────────────┐
│    SbomFeature      │
├─────────────────────┤
│ Id (PK)             │
│ Category            │
│ Feature             │
│ Ignored             │
│ DiscoveredAt        │
└─────────────────────┘
```

## Entities

### Sbom

Represents an uploaded Software Bill of Materials.

```csharp
public class Sbom
{
    public Guid Id { get; init; }
    public string Team { get; init; }
    public string Project { get; init; }
    public string Version { get; init; }
    public string SpecType { get; init; }      // "CycloneDX" or "SPDX"
    public string SpecVersion { get; init; }   // e.g., "1.5", "SPDX-2.3"
    public string SbomJson { get; init; }      // Stored as jsonb
    public int ComponentCount { get; init; }
    public DateTime UploadedAt { get; init; }
}
```

| Property | Description |
|----------|-------------|
| `Team` | Organizational grouping (e.g., "platform", "mobile") |
| `Project` | Project identifier within the team |
| `Version` | Build or release version |
| `SpecType` | SBOM format: `CycloneDX` or `SPDX` |
| `SpecVersion` | Format version (e.g., `1.5` for CycloneDX 1.5) |
| `SbomJson` | Raw SBOM content stored as PostgreSQL `jsonb` |
| `ComponentCount` | Number of components/packages in the SBOM |

### ValidationJob

Represents a unit of validation work to be processed.

```csharp
public class ValidationJob
{
    public Guid Id { get; init; }
    public Guid SbomId { get; init; }
    public ValidationJobStatus Status { get; set; }
    public string Profile { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; set; }
    public int RetryCount { get; set; }
    public string? FailureReason { get; set; }
}
```

| Property | Description |
|----------|-------------|
| `SbomId` | Foreign key to the SBOM being validated |
| `Status` | Current processing state (see below) |
| `Profile` | Validation profile to apply (e.g., "NIS2-Default") |
| `RetryCount` | Number of retry attempts (max 3) |
| `FailureReason` | Error message if job failed |

### ValidationResult

The outcome of a completed validation.

```csharp
public class ValidationResult
{
    public Guid Id { get; init; }
    public Guid ValidationJobId { get; init; }
    public ValidationStatus Status { get; init; }
    public double Score { get; init; }
    public string Profile { get; init; }
    public string ReportJson { get; init; }
    public DateTime CreatedAt { get; init; }
}
```

| Property | Description |
|----------|-------------|
| `ValidationJobId` | Links to the completed job |
| `Status` | `Pass` or `Fail` based on score threshold |
| `Score` | Quality score (0-100) from sbomqs |
| `ReportJson` | Full sbomqs report stored as `jsonb` |

### SbomFeature

Discovered quality features extracted from validation reports.

```csharp
public class SbomFeature
{
    public Guid Id { get; set; }
    public string Category { get; set; }
    public string Feature { get; set; }
    public bool Ignored { get; set; }
    public DateTime DiscoveredAt { get; set; }
}
```

This entity tracks the feature catalog as sbomqs evolves. New features are automatically discovered from validation reports.

## Enums

### ValidationJobStatus

```csharp
public enum ValidationJobStatus
{
    Pending = 0,     // Waiting for worker
    InProgress = 1,  // Being processed
    Completed = 2,   // Successfully finished
    Failed = 3       // Infrastructure failure (after 3 retries)
}
```

:::warning Important Distinction
`Failed` means the **job** failed due to infrastructure issues (sbomqs crash, database error). The SBOM itself may be fine. If validation runs successfully but the SBOM quality is poor, the job status is `Completed` but the result status is `Fail`.
:::

### ValidationStatus

```csharp
public enum ValidationStatus
{
    Pass = 1,  // Score >= threshold
    Fail = 2   // Score < threshold
}
```

## Database Schema

The EF Core configuration in `AppDbContext`:

```csharp
modelBuilder.Entity<Sbom>(entity =>
{
    entity.HasKey(x => x.Id);
    entity.Property(x => x.SbomJson).HasColumnType("jsonb");
});

modelBuilder.Entity<ValidationJob>(entity =>
{
    entity.HasKey(x => x.Id);
    entity.HasIndex(x => new { x.Status, x.CreatedAt });
});

modelBuilder.Entity<ValidationResult>(entity =>
{
    entity.HasKey(x => x.Id);
    entity.Property(x => x.ReportJson).HasColumnType("jsonb");
    entity.HasOne(x => x.ValidationJob)
        .WithMany()
        .HasForeignKey(x => x.ValidationJobId)
        .OnDelete(DeleteBehavior.Cascade);
    entity.HasIndex(x => x.ValidationJobId).IsUnique();
});

modelBuilder.Entity<SbomFeature>(entity =>
{
    entity.HasKey(x => x.Id);
    entity.HasIndex(x => x.Feature).IsUnique();
});
```

## Next Steps

- [Worker Service](./worker-service.md) — How jobs are processed
- [Extensibility](./extensibility.md) — Adding new validation tools
