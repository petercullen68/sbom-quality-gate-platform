using System.Net;
using NJsonSchema;
using SbomQualityGate.Infrastructure.Validation;
using SbomQualityGate.UnitTests.Helpers;

namespace SbomQualityGate.UnitTests.Infrastructure;

public class SpecConformanceToolTests
{
    // Minimal valid CycloneDX 1.4 schema — just enough for NJsonSchema to parse
    private const string MinimalCycloneDxSchema = """
        {
            "$schema": "http://json-schema.org/draft-07/schema#",
            "type": "object",
            "required": ["bomFormat", "specVersion"],
            "properties": {
                "bomFormat": { "type": "string" },
                "specVersion": { "type": "string" },
                "components": {
                    "type": "array",
                    "items": {
                        "type": "object",
                        "properties": {
                            "name": { "type": "string" },
                            "modified": { 
                                "type": "boolean",
                                "deprecated": true
                            }
                        }
                    }
                }
            }
        }
        """;

    private static SpecConformanceTool CreateTool(
        string schemaResponse,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        SchemaCache? cache = null)
    {
        var handler = new FakeHttpMessageHandler(schemaResponse, statusCode);
        var httpClient = new HttpClient(handler);
        var factory = new FakeHttpClientFactory(httpClient);
        cache ??= new SchemaCache();
        return new SpecConformanceTool(factory, cache);
    }

    [Fact]
    public async Task CheckAsyncUnknownSpecTypeReturnsNonConformant()
    {
        // Arrange
        var tool = CreateTool(MinimalCycloneDxSchema);

        // Act
        var result = await tool.CheckAsync("{}", "UnknownFormat", "1.0", CancellationToken.None);

        // Assert
        Assert.False(result.IsConformant);
        Assert.Single(result.Violations);
        Assert.Contains("No schema available", result.Violations[0]);
    }

    [Fact]
    public async Task CheckAsyncUnknownVersionReturnsNonConformant()
    {
        // Arrange
        var tool = CreateTool(MinimalCycloneDxSchema);

        // Act
        var result = await tool.CheckAsync("{}", "CycloneDX", "9.9", CancellationToken.None);

        // Assert
        Assert.False(result.IsConformant);
        Assert.Contains("No schema available", result.Violations[0]);
    }

    [Fact]
    public async Task CheckAsyncValidSbomReturnsConformant()
    {
        // Arrange
        var tool = CreateTool(MinimalCycloneDxSchema);
        var sbom = """{"bomFormat": "CycloneDX", "specVersion": "1.4"}""";

        // Act
        var result = await tool.CheckAsync(sbom, "CycloneDX", "1.4", CancellationToken.None);

        // Assert
        Assert.True(result.IsConformant);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public async Task CheckAsyncMissingRequiredFieldReturnsViolation()
    {
        // Arrange
        var tool = CreateTool(MinimalCycloneDxSchema);
        var sbom = """{"specVersion": "1.4"}"""; // missing required bomFormat

        // Act
        var result = await tool.CheckAsync(sbom, "CycloneDX", "1.4", CancellationToken.None);

        // Assert
        Assert.False(result.IsConformant);
        Assert.NotEmpty(result.Violations);
    }

    [Fact]
    public async Task CheckAsyncDeprecatedFieldInUseReturnsWarning()
    {
        // Arrange
        var tool = CreateTool(MinimalCycloneDxSchema);
        var sbom = """
            {
                "bomFormat": "CycloneDX",
                "specVersion": "1.4",
                "components": [
                    { "name": "acme-lib", "modified": true }
                ]
            }
            """;

        // Act
        var result = await tool.CheckAsync(sbom, "CycloneDX", "1.4", CancellationToken.None);

        // Assert
        Assert.True(result.IsConformant);
        Assert.NotEmpty(result.DeprecationWarnings);
        Assert.Contains(result.DeprecationWarnings, w => w.Contains("modified"));
    }

    [Fact]
    public async Task CheckAsyncNoDeprecatedFieldsInUseReturnsNoWarnings()
    {
        // Arrange
        var tool = CreateTool(MinimalCycloneDxSchema);
        var sbom = """
            {
                "bomFormat": "CycloneDX",
                "specVersion": "1.4",
                "components": [
                    { "name": "acme-lib" }
                ]
            }
            """;

        // Act
        var result = await tool.CheckAsync(sbom, "CycloneDX", "1.4", CancellationToken.None);

        // Assert
        Assert.True(result.IsConformant);
        Assert.Empty(result.DeprecationWarnings);
    }

    [Fact]
    public async Task CheckAsyncSchemaIsServedFromCacheOnSecondCall()
    {
        // Arrange
        var cache = new SchemaCache();
        var handler = new FakeHttpMessageHandler(MinimalCycloneDxSchema);
        var httpClient = new HttpClient(handler);
        var factory = new FakeHttpClientFactory(httpClient);
        var tool = new SpecConformanceTool(factory, cache);

        var sbom = """{"bomFormat": "CycloneDX", "specVersion": "1.4"}""";

        // Act — call twice
        var first = await tool.CheckAsync(sbom, "CycloneDX", "1.4", CancellationToken.None);
        var second = await tool.CheckAsync(sbom, "CycloneDX", "1.4", CancellationToken.None);

        // Assert — both succeed, and FetchedAt is identical (came from cache)
        Assert.True(first.IsConformant);
        Assert.True(second.IsConformant);
        Assert.Equal(first.FetchedAt, second.FetchedAt);
    }

    [Fact]
    public async Task CheckAsyncSpdxVersionNormalisedCorrectly()
    {
        // Arrange — SPDX specVersion is stored as "SPDX-2.3", tool must normalise it
        var tool = CreateTool(MinimalCycloneDxSchema);

        // Act — "SPDX-2.3" should resolve to a known version, not return "No schema available"
        // We use a schema that will pass validation for any JSON object
        var result = await tool.CheckAsync("{}", "SPDX", "SPDX-2.3", CancellationToken.None);

        // Assert — got past version resolution (no "No schema available" violation)
        Assert.DoesNotContain(result.Violations, v => v.Contains("No schema available"));
    }

    [Fact]
    public async Task CheckAsyncSchemaUrlIsPopulatedInResult()
    {
        // Arrange
        var tool = CreateTool(MinimalCycloneDxSchema);
        var sbom = """{"bomFormat": "CycloneDX", "specVersion": "1.4"}""";

        // Act
        var result = await tool.CheckAsync(sbom, "CycloneDX", "1.4", CancellationToken.None);

        // Assert
        Assert.NotEmpty(result.SchemaUrl);
        Assert.Contains("bom-1.4", result.SchemaUrl);
    }
}
