---
sidebar_position: 3
---

# Testing

SBOM Quality Gate uses a combination of unit tests and integration tests. This guide explains the testing approach and how to write effective tests.

## Test Projects

| Project | Purpose |
|---------|---------|
| `SbomQualityGate.UnitTests` | Fast, isolated tests for business logic |
| `SbomQualityGate.IntegrationTests` | End-to-end tests with real database |

## Running Tests

```bash
# Run all tests
dotnet test

# Run with verbosity
dotnet test --verbosity normal

# Run specific project
dotnet test tests/SbomQualityGate.UnitTests

# Run specific test
dotnet test --filter "FullyQualifiedName~HandleAsync_ValidSbom"

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Unit Tests

### Philosophy

Unit tests in this project:

- **Use fakes, not mocks**: Hand-written fake implementations, not Moq
- **Test behavior, not implementation**: Focus on inputs and outputs
- **Are fast**: No I/O, no database, run in milliseconds
- **Are isolated**: Each test is independent

### Fake Pattern

Instead of mocking frameworks, we use simple fake implementations:

```csharp title="tests/Fakes/FakeSbomRepository.cs"
public class FakeSbomRepository : ISbomRepository
{
    public bool AddCalled { get; private set; }
    public Sbom? AddedSbom { get; private set; }
    
    public Func<Guid, Sbom?>? GetByIdFunc { get; set; }

    public Task AddAsync(Sbom sbom, CancellationToken cancellationToken)
    {
        AddCalled = true;
        AddedSbom = sbom;
        return Task.CompletedTask;
    }

    public Task<Sbom?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(GetByIdFunc?.Invoke(id));
    }
}
```

**Benefits:**
- Clear, readable test setup
- Easy to inspect what was called
- No magic or reflection
- Compile-time safety

### Test Structure

Follow Arrange-Act-Assert:

```csharp
[Fact]
public async Task HandleAsync_ValidSbom_PersistsSbomAndCreatesJob()
{
    // Arrange
    var sbomRepo = new FakeSbomRepository();
    var jobRepo = new FakeValidationJobRepository();
    var unitOfWork = new FakeUnitOfWork();
    
    var handler = new SubmitSbomHandler(sbomRepo, jobRepo, unitOfWork);

    var command = new SubmitSbomCommand
    {
        Team = "platform",
        Project = "api",
        Version = "1.0.0",
        SbomJson = """{"bomFormat":"CycloneDX","specVersion":"1.5"}"""
    };

    // Act
    var result = await handler.HandleAsync(command, CancellationToken.None);

    // Assert
    Assert.NotEqual(Guid.Empty, result);
    Assert.True(sbomRepo.AddCalled);
    Assert.True(jobRepo.AddCalled);
    Assert.Equal(sbomRepo.AddedSbom!.Id, jobRepo.AddedJob!.SbomId);
}
```

### Naming Convention

`MethodName_Scenario_ExpectedResult`

Examples:
- `HandleAsync_ValidSbom_ReturnsId`
- `HandleAsync_InvalidJson_ThrowsRequestValidationException`
- `HandleAsync_JobCreationFails_DoesNotLeaveSbomOrphaned`

### Testing Exceptions

```csharp
[Fact]
public async Task HandleAsync_InvalidJson_ThrowsRequestValidationException()
{
    // Arrange
    var handler = CreateHandler();
    var command = new SubmitSbomCommand { SbomJson = "not-json" };

    // Act & Assert
    var ex = await Assert.ThrowsAsync<RequestValidationException>(
        () => handler.HandleAsync(command, CancellationToken.None));

    Assert.Contains("invalid JSON", ex.Message);
}
```

### Factory Methods

Use factory methods to reduce boilerplate:

```csharp
private static SubmitSbomHandler CreateHandler(
    ISbomRepository? sbomRepo = null,
    IValidationJobRepository? jobRepo = null,
    IUnitOfWork? unitOfWork = null)
{
    sbomRepo ??= new FakeSbomRepository();
    jobRepo ??= new FakeValidationJobRepository();
    unitOfWork ??= new FakeUnitOfWork();

    return new SubmitSbomHandler(sbomRepo, jobRepo, unitOfWork);
}
```

## Integration Tests

### Setup

Integration tests use `WebApplicationFactory` to spin up the API:

```csharp
public class SbomsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SbomsControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Upload_ValidSbom_Returns201()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        content.Add(new StringContent("""{"bomFormat":"CycloneDX",...}"""), "file", "sbom.json");
        content.Add(new StringContent("platform"), "team");
        content.Add(new StringContent("api"), "project");
        content.Add(new StringContent("1.0.0"), "version");

        // Act
        var response = await _client.PostAsync("/api/sboms/upload", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
```

### Database Configuration

Integration tests need a real PostgreSQL instance. Configure via `appsettings.Test.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=sbomqualitygate_test;..."
  }
}
```

### Test Database Isolation

Each test should clean up after itself or use transactions:

```csharp
public class DatabaseFixture : IDisposable
{
    public AppDbContext Context { get; }

    public DatabaseFixture()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(TestConnectionString)
            .Options;
        
        Context = new AppDbContext(options);
        Context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Context.Database.EnsureDeleted();
        Context.Dispose();
    }
}
```

### Stubbing External Tools

For CI environments where sbomqs isn't available, use `StubValidationTool`:

```csharp
public class StubValidationTool : IValidationTool
{
    public Task<ValidationToolResult> ValidateAsync(
        string sbomJson, 
        string profile, 
        CancellationToken ct)
    {
        return Task.FromResult(new ValidationToolResult
        {
            Status = ValidationStatus.Pass,
            Score = 85.0,
            ReportJson = """{"files":[{"sbom_quality_score":85}]}"""
        });
    }
}
```

Register in test setup:

```csharp
factory.WithWebHostBuilder(builder =>
{
    builder.ConfigureServices(services =>
    {
        services.AddScoped<IValidationTool, StubValidationTool>();
    });
});
```

## Test Coverage

### Generating Reports

```bash
dotnet test --collect:"XPlat Code Coverage"
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator \
  -reports:"**/coverage.cobertura.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:Html
```

### Coverage Goals

- **Domain**: High coverage (90%+) — pure logic
- **Application**: High coverage (85%+) — use case orchestration
- **Infrastructure**: Medium coverage (70%+) — external integrations harder to test
- **API**: Medium coverage (70%+) — focus on happy path and error handling

## CI Integration

Tests run on every PR:

```yaml title=".github/workflows/ci.yml"
- name: Test
  run: dotnet test --no-build --configuration Release --verbosity normal
```

All tests must pass before merge.

## Writing Good Tests

### Do

- ✅ Test one thing per test
- ✅ Use descriptive names
- ✅ Test edge cases
- ✅ Test error paths
- ✅ Keep tests fast

### Don't

- ❌ Test implementation details
- ❌ Share state between tests
- ❌ Use Thread.Sleep (use async properly)
- ❌ Ignore flaky tests (fix them)

## Next Steps

- [Contributing](./contributing) — Contribution guidelines
- [Local Setup](./local-setup) — Development environment
