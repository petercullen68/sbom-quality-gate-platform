---
sidebar_position: 3
---

# Configuration

SBOM Quality Gate uses the standard .NET configuration system. Settings can be provided via:

1. `appsettings.json` files
2. Environment variables
3. User secrets (development)
4. Command line arguments

## Connection String

The database connection string is required for both API and Worker services.

```json title="appsettings.json"
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=sbomqualitygate;Username=postgres;Password=secret"
  }
}
```

Environment variable equivalent:

```bash
export ConnectionStrings__Default="Host=localhost;Port=5432;..."
```

### Connection Pooling

For production, configure connection pooling:

```
Host=db.example.com;Database=sbomqualitygate;Username=app;Password=secret;Pooling=true;MinPoolSize=5;MaxPoolSize=100
```

## Upload Limits

Control the maximum allowed SBOM file size:

```json title="appsettings.json"
{
  "Upload": {
    "MaxUploadBytes": 10485760
  }
}
```

Default is 5 MB (`5242880` bytes). The limit applies to both the `/upload` multipart endpoint and the JSON body endpoint.

## Logging

SBOM Quality Gate uses Serilog for structured logging.

```json title="appsettings.json"
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/app-.log",
          "rollingInterval": "Day"
        }
      }
    ]
  }
}
```

### Log Levels

| Level | Use |
|-------|-----|
| `Verbose` | Detailed tracing (very noisy) |
| `Debug` | Development debugging |
| `Information` | Normal operational events |
| `Warning` | Unusual but handled situations |
| `Error` | Failures requiring attention |
| `Fatal` | Application crash |

## EF Core Retry Policy

The database connection is configured with automatic retry for transient failures:

```csharp
options.UseNpgsql(connectionString, npgsqlOptions =>
{
    npgsqlOptions.EnableRetryOnFailure(
        maxRetryCount: 5,
        maxRetryDelay: TimeSpan.FromSeconds(10),
        errorCodesToAdd: null);
});
```

This is built into the default configuration and requires no additional setup.

## Environment-Specific Configuration

Use environment-specific files for different deployments:

```
appsettings.json              # Base configuration
appsettings.Development.json  # Local development overrides
appsettings.Production.json   # Production overrides
```

Set the environment via `DOTNET_ENVIRONMENT` or `ASPNETCORE_ENVIRONMENT`:

```bash
export ASPNETCORE_ENVIRONMENT=Production
```

## Docker Configuration {#docker}

When running in Docker, pass configuration via environment variables:

```yaml title="docker-compose.yml"
services:
  api:
    image: sbomqualitygate-api
    environment:
      - ConnectionStrings__Default=Host=db;Database=sbomqualitygate;Username=postgres;Password=secret
      - Upload__MaxUploadBytes=10485760
      - ASPNETCORE_ENVIRONMENT=Production
    depends_on:
      - db

  worker:
    image: sbomqualitygate-worker
    environment:
      - ConnectionStrings__Default=Host=db;Database=sbomqualitygate;Username=postgres;Password=secret
      - DOTNET_ENVIRONMENT=Production
    depends_on:
      - db

  db:
    image: postgres:15
    environment:
      - POSTGRES_DB=sbomqualitygate
      - POSTGRES_PASSWORD=secret
    volumes:
      - postgres_data:/var/lib/postgresql/data

volumes:
  postgres_data:
```

## Kestrel (HTTP Server)

Configure the HTTP server for production:

```json title="appsettings.Production.json"
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5000"
      },
      "Https": {
        "Url": "https://0.0.0.0:5001",
        "Certificate": {
          "Path": "/certs/server.pfx",
          "Password": "certpassword"
        }
      }
    },
    "Limits": {
      "MaxRequestBodySize": 10485760
    }
  }
}
```

## OpenTelemetry (Future)

OpenTelemetry integration is planned. Configuration will be documented here when available.

## Next Steps

- [Architecture Overview](../architecture/overview) — Understand the system design
- [API Reference](../api) — Explore the API endpoints
