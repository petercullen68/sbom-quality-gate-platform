using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SbomQualityGate.Application.UseCases;
using SbomQualityGate.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace SbomQualityGate.IntegrationTests.Tests;

[Collection("IntegrationTests")]
public class ProcessValidationJobTests(SbomQualityGateApiFactory factory)
    : IntegrationTestBase(factory)
{
    private const string ReportJson = """
        {
          "files": [
            {
              "comprehenssive": [
                { "category": "Identification", "feature": "comp_with_name",    "score": 10, "ignored": false },
                { "category": "Identification", "feature": "comp_with_version", "score": 10, "ignored": false },
                { "category": "Provenance",     "feature": "sbom_authors",      "score": 0,  "ignored": false },
                { "category": "Integrity",      "feature": "sbom_signature",    "score": 0,  "ignored": true  }
              ],
              "profiles": [
                { "profile": "Interlynk", "score": 5.82, "grade": "D", "message": "Interlynk Scoring Profile" }
              ]
            }
          ]
        }
        """;

    private const string CycloneDxSbom = """
        {
            "bomFormat": "CycloneDX",
            "specVersion": "1.5",
            "components": []
        }
        """;

    private Guid _productId;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Seed profiles — required before SBOM submission
        await Client.PostAsJsonAsync(
            "/api/report/discover",
            JsonDocument.Parse(ReportJson).RootElement);

        _productId = await Factory.QueryAsync(ctx =>
            ctx.Products.Select(x => x.Id).FirstAsync());
    }

    [Fact]
    public async Task ProcessJobDiscoversNewFeaturesFromReport()
    {
        // Arrange — submit an SBOM to create a validation job
        var command = new SubmitSbomCommand
        {
            ProductId = _productId,
            Version = "1.0.0",
            SbomContent = CycloneDxSbom
        };

        await Client.PostAsJsonAsync("/api/sboms", command);

        // Remove some features to simulate stale catalogue
        await Factory.QueryAsync(async ctx =>
        {
            var toRemove = await ctx.SbomFeatures
                .Where(x => x.Feature == "comp_with_name" || x.Feature == "sbom_authors")
                .ToListAsync();

            ctx.SbomFeatures.RemoveRange(toRemove);
            await ctx.SaveChangesAsync();
            return true;
        });

        // Confirm they're gone
        var beforeCount = await Factory.QueryAsync(ctx =>
            ctx.SbomFeatures.CountAsync());

        Assert.Equal(2, beforeCount);

        // Act — run the validation job handler directly
        using var scope = Factory.Services.CreateScope();
        var handler = scope.ServiceProvider
            .GetRequiredService<ProcessNextValidationJobHandler>();

        var processed = await handler.HandleAsync(CancellationToken.None);
        Assert.True(processed);

        // Assert — removed features are back
        var features = await Factory.QueryAsync(ctx =>
            ctx.SbomFeatures.Select(x => x.Feature).ToListAsync());

        Assert.Contains("comp_with_name", features);
        Assert.Contains("sbom_authors", features);
    }

    [Fact]
    public async Task ProcessJobDoesNotDuplicateExistingFeatures()
    {
        // Arrange
        var command = new SubmitSbomCommand
        {
            ProductId = _productId,
            Version = "1.0.0",
            SbomContent = CycloneDxSbom
        };

        await Client.PostAsJsonAsync("/api/sboms", command);

        var beforeCount = await Factory.QueryAsync(ctx =>
            ctx.SbomFeatures.CountAsync());

        // Act
        using var scope = Factory.Services.CreateScope();
        var handler = scope.ServiceProvider
            .GetRequiredService<ProcessNextValidationJobHandler>();

        await handler.HandleAsync(CancellationToken.None);

        // Assert — count unchanged
        var afterCount = await Factory.QueryAsync(ctx =>
            ctx.SbomFeatures.CountAsync());

        Assert.Equal(beforeCount, afterCount);
    }
}
