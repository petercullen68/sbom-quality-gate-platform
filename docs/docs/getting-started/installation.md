---
sidebar_position: 1
---

# Installation

SBOM Quality Gate consists of two components:

1. **API** — The HTTP service that accepts SBOM uploads and serves results
2. **Worker** — The background service that processes validation jobs

Both require a PostgreSQL database and the `sbomqs` CLI tool.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL 15+](https://www.postgresql.org/download/)
- [sbomqs](https://github.com/interlynk-io/sbomqs) CLI tool

### Installing sbomqs

```bash
# Using Homebrew (macOS/Linux)
brew install interlynk-io/tap/sbomqs

# Or download the binary directly
curl -sSfL https://raw.githubusercontent.com/interlynk-io/sbomqs/main/install.sh | sh

# Verify installation
sbomqs version
```

## Database Setup

Create a PostgreSQL database:

```bash
# Using psql
psql -U postgres -c "CREATE DATABASE sbomqualitygate;"

# Or using Docker
docker run -d \
  --name sbomqg-postgres \
  -e POSTGRES_DB=sbomqualitygate \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=yourpassword \
  -p 5432:5432 \
  postgres:15
```

## Clone and Build

```bash
git clone https://github.com/petercullen68/sbom-quality-gate-platform.git
cd SbomQualityGate

# Restore dependencies
dotnet restore

# Build the solution
dotnet build --configuration Release
```

## Configuration

Both the API and Worker read configuration from `appsettings.json` and environment variables.

### Connection String

Set your database connection string:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=sbomqualitygate;Username=postgres;Password=yourpassword"
  }
}
```

Or via environment variable:

```bash
export ConnectionStrings__Default="Host=localhost;Database=sbomqualitygate;..."
```

### Upload Limits

Configure maximum SBOM upload size:

```json
{
  "Upload": {
    "MaxUploadBytes": 5242880
  }
}
```

## Running the Services

### Run Migrations

```bash
cd src/SbomQualityGate.Api
dotnet ef database update
```

### Start the API

```bash
cd src/SbomQualityGate.Api
dotnet run
```

The API will be available at `https://localhost:5001` (or `http://localhost:5000`).

### Start the Worker

In a separate terminal:

```bash
cd src/SbomQualityGate.Worker
dotnet run
```

## Docker Deployment

A Docker Compose setup is available for running the complete stack:

```bash
docker compose up -d
```

This starts:
- PostgreSQL database
- SBOM Quality Gate API
- SBOM Quality Gate Worker

See [Docker Deployment](./configuration#docker) for details.

## Next Steps

- [Quick Start](./quick-start) — Submit your first SBOM
- [Configuration](./configuration) — Detailed configuration options
