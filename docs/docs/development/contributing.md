---
sidebar_position: 1
---

# Contributing

Thank you for your interest in contributing to SBOM Quality Gate! This guide will help you get started.

## Code of Conduct

Please be respectful and constructive in all interactions. We're building something useful together.

## Ways to Contribute

### Report Issues

Found a bug? Have a feature request?

1. Search [existing issues](https://github.com/petercullen68/sbom-quality-gate-platform/issues) first
2. If not found, [create a new issue](https://github.com/petercullen68/sbom-quality-gate-platform/issues/new)
3. Include:
   - Clear title and description
   - Steps to reproduce (for bugs)
   - Expected vs actual behavior
   - Environment details (.NET version, OS, etc.)

### Submit Pull Requests

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/my-feature`
3. Make your changes
4. Run tests: `dotnet test`
5. Commit with clear messages
6. Push and create a Pull Request

### Improve Documentation

Documentation lives in the `/docs` directory. Improvements are always welcome:

- Fix typos
- Clarify confusing sections
- Add examples
- Translate content

## Development Workflow

### Branch Strategy

- `main` — Production-ready code
- `develop` — Integration branch (if used)
- `feature/*` — New features
- `fix/*` — Bug fixes

### Commit Messages

Use clear, descriptive commit messages:

```
feat: Add validation profile support

- Implement ConformancePolicy entity
- Add policy evaluation logic
- Create migration for new tables
```

Prefixes:
- `feat:` — New feature
- `fix:` — Bug fix
- `docs:` — Documentation
- `test:` — Tests
- `refactor:` — Code restructuring
- `chore:` — Build, tooling, etc.

### Pull Request Process

1. **CI must pass**: All tests green, no warnings-as-errors
2. **Review required**: PRs require at least one approval
3. **Squash merge**: Keep history clean

## Code Style

### C# Conventions

- Primary constructors where appropriate
- `LoggerMessage.Define` for high-performance logging
- Async suffix on async methods (`HandleAsync`, not `Handle`)
- Interfaces in Application layer, implementations in Infrastructure

### Formatting

The solution uses standard .NET formatting. Run before committing:

```bash
dotnet format
```

### Analyzers

Warnings are treated as errors in Release builds. Address all analyzer warnings.

## Testing

### Unit Tests

- Located in `SbomQualityGate.UnitTests`
- Use fakes, not mocks (see `/Fakes` directory)
- Name: `MethodName_Scenario_ExpectedResult`

```csharp
[Fact]
public async Task HandleAsync_ValidSbom_CreatesJobAndReturnsId()
{
    // Arrange
    var handler = CreateHandler();
    
    // Act
    var result = await handler.HandleAsync(command, CancellationToken.None);
    
    // Assert
    Assert.NotEqual(Guid.Empty, result);
}
```

### Integration Tests

- Located in `SbomQualityGate.IntegrationTests`
- Use `WebApplicationFactory` for API tests
- Require PostgreSQL (Docker recommended)

### Running Tests

```bash
# All tests
dotnet test

# Specific project
dotnet test tests/SbomQualityGate.UnitTests

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Architecture Guidelines

### Layer Boundaries

- **Domain**: No external dependencies
- **Application**: Orchestrates use cases, defines interfaces
- **Infrastructure**: Implements interfaces, external concerns
- **API/Worker**: Thin, delegates to Application layer

### Adding a New Feature

1. Start with the Domain entity (if needed)
2. Define interface in Application
3. Implement use case handler in Application
4. Implement infrastructure concerns
5. Wire up in DI container
6. Add API endpoint (if needed)
7. Write tests at each layer

### Adding a New Validation Tool

See [Extensibility](../architecture/extensibility) for the `IValidationTool` pattern.

## Getting Help

- [GitHub Discussions](https://github.com/petercullen68/sbom-quality-gate-platform/discussions) — Questions, ideas
- [Issues](https://github.com/petercullen68/sbom-quality-gate-platform/issues) — Bugs, feature requests

## License

By contributing, you agree that your contributions will be licensed under the Apache License 2.0.
