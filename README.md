# SBOM Quality Gate Platform

![CI](https://github.com/petercullen68/sbom-quality-gate-platform/actions/workflows/ci.yml/badge.svg)

## Overview

The SBOM Quality Gate Platform is a backend system designed to:

- Ingest Software Bill of Materials (SBOM) documents
- Validate SBOM quality using external tooling (sbomqs)
- Score SBOMs against industry compliance profiles (NTIA, BSI TR-03183-2, OpenChain Telco and others)
- Provide auditability and visibility into SBOM quality across teams and products

Built with a strong emphasis on clean architecture, testability, and extensibility. A second validation tool implementation slots in by implementing two interfaces — nothing else changes.

---

## Architecture

The system follows a layered architecture:

```
API → Application → Domain
             ↓
     Infrastructure (DB, external tools)
             ↓
           Worker
```

### API

- Handles HTTP requests (SBOM upload, submission, report discovery)
- Performs input validation and request shaping
- Does not perform heavy processing

### Application

- Contains use cases (handlers)
- Coordinates workflows (submit SBOM, process job, discover report)
- Depends only on interfaces — no infrastructure coupling

### Domain

- Core entities: `Team`, `Product`, `Sbom`, `ValidationJob`, `ValidationResult`, `SbomFeature`, `SbomProfile`
- Business enums and rules

### Infrastructure

- EF Core persistence (PostgreSQL)
- External tool integration (`sbomqs`)
- Process execution abstraction (`IProcessRunner`)
- Report discovery abstraction (`IReportDiscoveryTool`)
- `UnitOfWork` implementation
- OpenTelemetry instrumentation

### Worker

- Background service
- Listens for DB notifications (LISTEN/NOTIFY) with fallback polling
- Processes validation jobs asynchronously
- Handles retries, backoff, and circuit breaking

---

## Core Flow

```
API → DB → Job → Worker → sbomqs → Result
```

1. SBOM submitted via API against a known Product
2. System validates profiles have been discovered — hard rejects if not
3. SBOM stored, validation job created (Pending)
4. Worker picks up job via SKIP LOCKED claim pattern
5. sbomqs executed against SBOM
6. Result stored, job marked Completed
7. Profile scores stored against result

---

## Data Model

```
Team ──< TeamMember >── User (Identity — future)
  │
Product
  │
 Sbom
  │
ValidationJob
  │
ValidationResult
  │
ValidationProfileResult (future)

SbomProfile  (system-defined, discovered dynamically from sbomqs reports)
SbomFeature  (discovered dynamically from sbomqs reports)
```

Teams own Products. Products have versioned SBOM submissions. Each submission creates a validation job processed by the Worker.

---

## Key Design Decisions

**API / Worker separation** — API is fast and responsive. Worker handles heavy processing. Enables independent scaling.

**`IValidationTool` abstraction** — sbomqs is pluggable. A second tool implements the interface and registers itself. Nothing else changes.

**`IReportDiscoveryTool` abstraction** — report parsing is equally pluggable. sbomqs-specific JSON parsing lives in `SbomQsReportDiscoveryTool`, not in the application layer.

**`IUnitOfWork` abstraction** — centralised transaction handling. Testable without EF dependency.

**Dynamic profile discovery** — compliance profiles (NTIA, BSI, OpenChain etc.) are discovered from sbomqs reports at runtime, not hardcoded. New profiles appear automatically when sbomqs adds them.

**Hard reject guards** — if profiles haven't been discovered, SBOM submission is rejected with a clear message. Data consistency is enforced at the boundary.

**Circuit breaker** — sbomqs failures are tracked. After 5 consecutive failures the circuit opens for 1 minute before retrying.

---

## Tool Abstraction

Any SBOM quality tool can be integrated by implementing two interfaces:

```csharp
// Score an SBOM
public interface IValidationTool
{
    Task<ValidationToolResult> ValidateAsync(
        string sbomJson,
        string profile,
        CancellationToken cancellationToken);
}

// Parse a report for feature and profile discovery
public interface IReportDiscoveryTool
{
    ReportDiscoveryResult Discover(string reportJson);
}
```

Register your implementations and the rest of the system is unaware of the underlying tool.

---

## Testing Strategy

### Unit Tests (`/tests/UnitTests`)

- Fakes over mocks — no DB, no external processes
- Full coverage of all handlers and failure paths
- Tests run on Ubuntu and Windows in CI

### Integration Tests (`/tests/IntegrationTests`)

- Real PostgreSQL instance
- `WebApplicationFactory<Program>` — full API stack in-process
- Migrations run once at fixture startup
- Database reset between tests
- 17 tests covering report discovery, SBOM submission (JSON and file upload)

---

## CI Pipeline

GitHub Actions runs on every push and pull request:

- **Secrets scan** — gitleaks on full history
- **Build + Unit Tests** — Ubuntu and Windows
- **Vulnerable package check** — `dotnet list package --vulnerable`
- **Integration Tests** — Ubuntu with PostgreSQL service container

---

## Observability

OpenTelemetry instrumentation in both API and Worker:

- ASP.NET Core tracing and metrics (API)
- EF Core query tracing (both)
- .NET runtime metrics (both)
- OTLP export — configurable endpoint, defaults to `http://localhost:4317`

Compatible with Jaeger, Grafana Tempo, Honeycomb, or any OTLP-compatible backend.

---

## Getting Started

### Prerequisites

- .NET 10 SDK
- PostgreSQL
- sbomqs installed and on PATH

### Configuration

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=sbomdb;Username=postgres;Password=yourpassword"
  },
  "OpenTelemetry": {
    "OtlpEndpoint": "http://localhost:4317"
  }
}
```

### Running

```bash
# API
./run-api.sh

# Worker
./run-worker.sh
```

### Before submitting SBOMs

The discovery endpoint must be called first with a sbomqs report to populate compliance profiles:

```bash
POST /api/report/discover
```

---

## Current Status

| Feature | Status |
|---|---|
| SBOM ingestion | ✅ |
| Job processing | ✅ |
| sbomqs integration | ✅ |
| Feature discovery | ✅ |
| Profile discovery | ✅ |
| Team / Product hierarchy | ✅ |
| Unit tests | ✅ |
| Integration tests | ✅ |
| OpenTelemetry | ✅ |
| ValidationProfileResults | 🚧 |
| User-defined scoring | 🚧 |
| .NET Core Identity | 🚧 |
| API key authentication | 🚧 |
| Team-scoped permissions | 🚧 |

---

## Design Philosophy

- Simplicity over cleverness
- Explicit boundaries over hidden coupling
- Testability as a first-class concern
- Hard reject early when data consistency is at risk
- Incremental evolution over big rewrites