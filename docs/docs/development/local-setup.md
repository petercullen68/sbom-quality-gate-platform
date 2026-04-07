---
sidebar_position: 2
---

# Local Development Setup

This guide walks through setting up a complete development environment.

## Prerequisites

### Required

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [PostgreSQL 15+](https://www.postgresql.org/download/)
- [sbomqs](https://github.com/interlynk-io/sbomqs)

### Recommended

- [Docker](https://www.docker.com/products/docker-desktop) — For running PostgreSQL
- [pgModeler](https://pgmodeler.io/) — Database visualization (optional)
- [VS Code](https://code.visualstudio.com/) or [JetBrains Rider](https://www.jetbrains.com/rider/)

## Quick Start with Docker

The fastest way to get a development environment:

```bash
# Clone the repository
git clone https://github.com/petercullen68/sbom-quality-gate-platform.git
cd SbomQualityGate

# Start PostgreSQL
docker run -d \
  --name sbomqg-postgres \
  -e POSTGRES_DB=sbomqualitygate \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=devpassword \
  -p 5432:5432 \
  postgres:15

# Verify database is running
docker logs sbomqg-postgres
```

## Install sbomqs

sbomqs is required for validation:

```bash
# macOS/Linux with Homebrew
brew install interlynk-io/tap/sbomqs

# Or download directly
curl -sSfL https://raw.githubusercontent.com/interlynk-io/sbomqs/main/install.sh | sh

# Verify
sbomqs version
```

## Configure the Application

### Connection String

Create or update `appsettings.Development.json` in both API and Worker projects:

```json title="src/SbomQualityGate.Api/appsettings.Development.json"
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=sbomqualitygate;Username=postgres;Password=devpassword"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```

### User Secrets (Alternative)

For sensitive data, use .NET user secrets:

```bash
cd src/SbomQualityGate.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;..."
```

## Build and Run

### Restore Dependencies

```bash
dotnet restore
```

### Apply Migrations

```bash
cd src/SbomQualityGate.Api
dotnet ef database update
```

### Run the API

```bash
cd src/SbomQualityGate.Api
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger: `https://localhost:5001/swagger`

### Run the Worker

In a separate terminal:

```bash
cd src/SbomQualityGate.Worker
dotnet run
```

## Verify Everything Works

### Submit a Test SBOM

```bash
# Create a minimal test SBOM
cat > test-sbom.json << 'EOF'
{
  "bomFormat": "CycloneDX",
  "specVersion": "1.5",
  "version": 1,
  "components": [
    {
      "type": "library",
      "name": "test-component",
      "version": "1.0.0"
    }
  ]
}
EOF

# Submit it
curl -X POST https://localhost:5001/api/sboms/upload \
  -k \
  -F "file=@test-sbom.json" \
  -F "team=dev" \
  -F "project=test" \
  -F "version=1.0.0"
```

### Check Worker Logs

The worker should pick up the job and process it:

```
[INF] Worker started. Listening for notifications...
[INF] Notification received - processing jobs
[INF] Validation completed: Score=65.5, Status=Fail
```

## IDE Setup

### VS Code

Recommended extensions:

- C# Dev Kit
- PostgreSQL (by Chris Kolkman)
- REST Client

Create `.vscode/launch.json`:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "API",
      "type": "coreclr",
      "request": "launch",
      "program": "${workspaceFolder}/src/SbomQualityGate.Api/bin/Debug/net10.0/SbomQualityGate.Api.dll",
      "cwd": "${workspaceFolder}/src/SbomQualityGate.Api"
    },
    {
      "name": "Worker",
      "type": "coreclr",
      "request": "launch",
      "program": "${workspaceFolder}/src/SbomQualityGate.Worker/bin/Debug/net10.0/SbomQualityGate.Worker.dll",
      "cwd": "${workspaceFolder}/src/SbomQualityGate.Worker"
    }
  ]
}
```

### JetBrains Rider

- Open the `.slnx` solution file
- Configure run configurations for API and Worker
- Use the built-in database tools for PostgreSQL

## Common Issues

### Connection Refused to PostgreSQL

```
Npgsql.NpgsqlException: Failed to connect to localhost:5432
```

**Fix**: Ensure PostgreSQL is running:
```bash
docker ps | grep postgres
# If not running:
docker start sbomqg-postgres
```

### sbomqs Not Found

```
System.ComponentModel.Win32Exception: No such file or directory
```

**Fix**: Ensure sbomqs is in your PATH:
```bash
which sbomqs
# If not found, add to PATH or install
```

### Migration Errors

```
The entity type 'Sbom' requires a primary key to be defined.
```

**Fix**: Ensure you're running migrations from the API project:
```bash
cd src/SbomQualityGate.Api
dotnet ef database update
```

### Port Already in Use

```
System.IO.IOException: Failed to bind to address https://127.0.0.1:5001
```

**Fix**: Kill the existing process or use a different port:
```bash
lsof -i :5001 | grep LISTEN
kill -9 <PID>
```

## Database Management

### Reset Database

```bash
docker stop sbomqg-postgres
docker rm sbomqg-postgres
# Then recreate with the docker run command above
```

### View Data

```bash
docker exec -it sbomqg-postgres psql -U postgres -d sbomqualitygate

# List tables
\dt

# Query SBOMs
SELECT id, team, project, version FROM "Sboms";

# Exit
\q
```

## Next Steps

- [Testing](./testing.md) — Running and writing tests
- [Contributing](./contributing.md) — Contribution guidelines
