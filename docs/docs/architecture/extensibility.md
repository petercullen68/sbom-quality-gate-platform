---
sidebar_position: 4
---

# Extensibility

SBOM Quality Gate is designed to integrate with multiple validation and analysis tools. The `IValidationTool` abstraction is the primary extension point.

## IValidationTool Interface

```csharp
public interface IValidationTool
{
    Task<ValidationToolResult> ValidateAsync(
        string sbomJson,
        string profile,
        CancellationToken cancellationToken);
}

public class ValidationToolResult
{
    public ValidationStatus Status { get; init; }  // Pass or Fail
    public double Score { get; init; }             // 0-100
    public string ReportJson { get; init; }        // Tool-specific output
}
```

## Implementing a Custom Validation Tool

Here's an example of wrapping a hypothetical "sbom-scorecard" tool:

```csharp
public class SbomScorecardTool : IValidationTool
{
    private readonly IProcessRunner _processRunner;

    public SbomScorecardTool(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    public async Task<ValidationToolResult> ValidateAsync(
        string sbomJson,
        string profile,
        CancellationToken cancellationToken)
    {
        // Write SBOM to temp file
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, sbomJson, cancellationToken);

        try
        {
            var (exitCode, output, error) = await _processRunner.RunAsync(
                "sbom-scorecard",
                $"--input {tempFile} --format json",
                cancellationToken);

            if (exitCode != 0)
            {
                throw new ValidationToolException(
                    $"sbom-scorecard failed: {error}",
                    exitCode);
            }

            // Parse tool output
            var doc = JsonDocument.Parse(output);
            var score = doc.RootElement.GetProperty("score").GetDouble();

            return new ValidationToolResult
            {
                Status = score >= 80 ? ValidationStatus.Pass : ValidationStatus.Fail,
                Score = score,
                ReportJson = output
            };
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
```

## Registration

Register your tool in the DI container:

```csharp
// Single tool (replaces default)
builder.Services.AddScoped<IValidationTool, SbomScorecardTool>();

// Or use a factory for multiple tools (future)
builder.Services.AddScoped<IValidationTool>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var tool = config["ValidationTool"];

    return tool switch
    {
        "sbomqs" => new SbomQsValidationTool(sp.GetRequiredService<IProcessRunner>()),
        "scorecard" => new SbomScorecardTool(sp.GetRequiredService<IProcessRunner>()),
        _ => throw new InvalidOperationException($"Unknown tool: {tool}")
    };
});
```

## Best Practices

### 1. Use IProcessRunner

Don't spawn processes directly. Use the `IProcessRunner` abstraction:
- Enables unit testing with fakes
- Built-in timeout handling (30 seconds)
- Consistent error handling

### 2. Implement Circuit Breaker

For external tools that may fail, implement a circuit breaker pattern:

```csharp
private int _failureCount;
private DateTime _blockedUntil = DateTime.MinValue;

public async Task<ValidationToolResult> ValidateAsync(...)
{
    if (DateTime.UtcNow < _blockedUntil)
    {
        throw new InvalidOperationException("Tool temporarily unavailable");
    }

    try
    {
        var result = await DoValidation(...);
        _failureCount = 0;
        return result;
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
}
```

### 3. Clean Up Temp Files

Always clean up temporary files in a `finally` block:

```csharp
var tempFile = Path.GetTempFileName();
try
{
    // ... use temp file
}
finally
{
    try { File.Delete(tempFile); }
    catch { /* ignore cleanup errors */ }
}
```

### 4. Structured Report Output

Store the raw tool output in `ReportJson`. This preserves all details for:
- Feature discovery
- Detailed result viewing
- Future analysis

## Future: Multiple Tool Orchestration

The roadmap includes support for running multiple tools per SBOM:

```
SBOM Upload
    │
    ├──▶ sbomqs (quality scoring)
    │
    ├──▶ grype (vulnerability scan)
    │
    └──▶ licensee (license compliance)
          │
          ▼
    Aggregated Result
```

This will likely involve:
- A new `IValidationToolOrchestrator` interface
- Parallel execution with individual results
- Configurable fail-fast vs. run-all behavior

## Dependency-Track Integration

Dependency-Track integration is planned but has unique challenges:

- **Async**: Analysis takes minutes, not seconds
- **Stateful**: Results are stored in D-Track, not returned inline
- **Polling**: Must poll for completion

This may warrant a separate abstraction (`IDependencyAnalysisTool`) rather than forcing it into `IValidationTool`.

## Testing Your Tool

Create a fake implementation for unit tests:

```csharp
public class FakeValidationTool : IValidationTool
{
    public bool WasCalled { get; private set; }
    public bool ShouldThrow { get; init; }
    public ValidationToolResult ResultToReturn { get; init; } = new()
    {
        Status = ValidationStatus.Pass,
        Score = 95,
        ReportJson = "{}"
    };

    public Task<ValidationToolResult> ValidateAsync(
        string sbomJson,
        string profile,
        CancellationToken cancellationToken)
    {
        WasCalled = true;

        if (ShouldThrow)
            throw new ValidationToolException("Simulated failure", -1);

        return Task.FromResult(ResultToReturn);
    }
}
```

## Next Steps

- [API Reference](../api) — Understand how jobs flow through the system
- [CI/CD Integration](../integrations/ci-cd) — End-to-end pipeline setup
