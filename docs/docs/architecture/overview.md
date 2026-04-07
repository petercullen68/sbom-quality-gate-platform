---
sidebar_position: 1
---

# Architecture Overview

SBOM Quality Gate follows a clean architecture pattern with clear separation between domain logic, application use cases, infrastructure concerns, and presentation.

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                        Presentation Layer                            │
│  ┌─────────────────┐                      ┌──────────────────────┐  │
│  │   API           │                      │   Worker Service     │  │
│  │   (ASP.NET)     │                      │   (BackgroundService)│  │
│  └────────┬────────┘                      └──────────┬───────────┘  │
└───────────┼──────────────────────────────────────────┼──────────────┘
            │                                          │
            ▼                                          ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        Application Layer                             │
│  ┌─────────────────────┐    ┌─────────────────────────────────────┐ │
│  │ SubmitSbomHandler   │    │ ProcessNextValidationJobHandler    │ │
│  │ DiscoverFeatures... │    │                                     │ │
│  └─────────────────────┘    └─────────────────────────────────────┘ │
│                                                                      │
│  Interfaces: ISbomRepository, IValidationTool, IUnitOfWork, etc.    │
└──────────────────────────────┬──────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        Domain Layer                                  │
│  Entities: Sbom, ValidationJob, ValidationResult, SbomFeature       │
│  Enums: ValidationStatus, ValidationJobStatus                        │
└─────────────────────────────────────────────────────────────────────┘
                               ▲
                               │
┌──────────────────────────────┴──────────────────────────────────────┐
│                        Infrastructure Layer                          │
│  ┌────────────────┐  ┌───────────────┐  ┌─────────────────────────┐ │
│  │ Persistence    │  │ Validation    │  │ Process                 │ │
│  │ (EF Core)      │  │ (sbomqs)      │  │ (CLI wrapper)           │ │
│  └────────────────┘  └───────────────┘  └─────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
```

## Project Structure

```
SbomQualityGate/
├── src/
│   ├── SbomQualityGate.Domain/        # Entities, enums, domain logic
│   ├── SbomQualityGate.Application/   # Use cases, interfaces
│   ├── SbomQualityGate.Infrastructure/# Repositories, external tools
│   ├── SbomQualityGate.Api/           # HTTP API
│   └── SbomQualityGate.Worker/        # Background job processor
├── tests/
│   ├── SbomQualityGate.UnitTests/
│   └── SbomQualityGate.IntegrationTests/
└── docs/                              # This documentation
```

## Component Responsibilities

### Domain Layer

Pure domain entities with no external dependencies:

- **`Sbom`**: The uploaded SBOM document with metadata
- **`ValidationJob`**: A unit of work to be processed
- **`ValidationResult`**: The outcome of a validation job
- **`SbomFeature`**: Discovered quality features from reports

### Application Layer

Orchestrates use cases and defines contracts:

- **`SubmitSbomHandler`**: Accepts SBOMs, creates validation jobs
- **`ProcessNextValidationJobHandler`**: Claims and processes pending jobs
- **`DiscoverSbomFeaturesHandler`**: Extracts feature metadata from reports

Key interfaces:
- **`IValidationTool`**: Abstraction for validation tools (sbomqs, future tools)
- **`ISbomRepository`**, **`IValidationJobRepository`**: Data access contracts
- **`IUnitOfWork`**: Transaction management

### Infrastructure Layer

Implements external concerns:

- **`SbomQsValidationTool`**: Wraps the sbomqs CLI with circuit breaker
- **`*Repository`**: EF Core implementations
- **`ProcessRunner`**: Safe process execution with timeout

### API Layer

RESTful HTTP interface:

- `POST /api/sboms` — Submit SBOM via JSON
- `POST /api/sboms/upload` — Submit SBOM via file upload
- `GET /api/sboms/{id}` — Retrieve SBOM metadata

### Worker Layer

Long-running background processor:

- **`Worker`**: BackgroundService that listens for jobs
- **`JobProcessor`**: Drains the job queue
- **`PostgresNotificationListener`**: LISTEN/NOTIFY for real-time triggers

## Data Flow

### SBOM Submission

```
1. Client → POST /api/sboms/upload
2. SbomsController validates request
3. SubmitSbomHandler:
   a. Parses SBOM JSON (extracts specType, version, component count)
   b. Creates Sbom entity
   c. Creates ValidationJob (status: Pending)
   d. Commits transaction
   e. Sends NOTIFY validation_jobs
4. Returns 201 Created with SBOM ID
```

### Job Processing

```
1. PostgresNotificationListener receives NOTIFY
2. Worker wakes up, calls JobProcessor
3. ProcessNextValidationJobHandler:
   a. Claims next pending job (SELECT FOR UPDATE SKIP LOCKED)
   b. Loads associated Sbom
   c. Calls IValidationTool.ValidateAsync
   d. Creates ValidationResult
   e. Marks job Completed
4. Loop continues until no pending jobs
```

## Key Design Decisions

### Async Job Processing

SBOMs are validated asynchronously to:
- Keep uploads fast (sub-second response)
- Allow retries without losing client connection
- Enable scaling workers independently

### PostgreSQL LISTEN/NOTIFY

Real-time job notification without polling:
- API sends `NOTIFY validation_jobs` after committing a new job
- Worker receives notification immediately
- Fallback polling every 30 seconds handles edge cases

### Circuit Breaker on sbomqs

The `SbomQsValidationTool` implements a circuit breaker:
- After 5 consecutive failures, blocks calls for 1 minute
- Prevents cascading failures if sbomqs is down
- Resets on successful validation

### Unit of Work Pattern

All database operations go through `IUnitOfWork`:
- Single transaction per use case
- Automatic retry on transient failures (EF Core execution strategy)
- Optional `NOTIFY` trigger after commit

## Next Steps

- [Domain Model](./domain-model) — Detailed entity documentation
- [Worker Service](./worker-service) — Background processing details
- [Extensibility](./extensibility) — Adding new validation tools
