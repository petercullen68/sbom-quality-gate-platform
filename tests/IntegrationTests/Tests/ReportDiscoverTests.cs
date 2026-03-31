using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace SbomQualityGate.IntegrationTests.Tests;

public class ReportDiscoverTests : IntegrationTestBase
{
    private static readonly string ReportJson = """
        {
          "run_id": "59da3d1a-27c0-4c83-a87b-fb8970e9bc54",
          "timestamp": "2026-03-22T15:17:02Z",
          "files": [
            {
              "sbom_quality_score": 5.54,
              "grade": "D",
              "num_components": 1600,
              "comprehenssive": [
                { "category": "Identification", "feature": "comp_with_name",    "score": 10, "ignored": false },
                { "category": "Identification", "feature": "comp_with_version", "score": 10, "ignored": false },
                { "category": "Provenance",     "feature": "sbom_authors",      "score": 0,  "ignored": false },
                { "category": "Integrity",      "feature": "sbom_signature",    "score": 0,  "ignored": true  }
              ],
              "profiles": [
                { "profile": "Interlynk",                    "score": 5.82, "grade": "D", "message": "Interlynk Scoring Profile" },
                { "profile": "NTIA Minimum Elements (2021)", "score": 8.57, "grade": "B", "message": "NTIA Minimum Elements Profile" },
                { "profile": "BSI TR-03183-2 v1.1",          "score": 6.36, "grade": "D", "message": "BSI TR-03183-2 v1.1 Profile" }
              ]
            }
          ]
        }
        """;

    [Fact]
    public async Task DiscoverReportReturns200()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/report/discover",
            JsonDocument.Parse(ReportJson).RootElement);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DiscoverReportPersistsFeatures()
    {
        await Client.PostAsJsonAsync(
            "/api/report/discover",
            JsonDocument.Parse(ReportJson).RootElement);

        var features = await Factory.QueryAsync(ctx =>
            ctx.SbomFeatures.Select(x => x.Feature).ToListAsync());

        Assert.Contains("comp_with_name", features);
        Assert.Contains("comp_with_version", features);
        Assert.Contains("sbom_authors", features);
        Assert.Contains("sbom_signature", features);
    }

    [Fact]
    public async Task DiscoverReportPersistsProfiles()
    {
        await Client.PostAsJsonAsync(
            "/api/report/discover",
            JsonDocument.Parse(ReportJson).RootElement);

        var profiles = await Factory.QueryAsync(ctx =>
            ctx.SbomProfiles.Select(x => x.Name).ToListAsync());

        Assert.Contains("Interlynk", profiles);
        Assert.Contains("NTIA Minimum Elements (2021)", profiles);
        Assert.Contains("BSI TR-03183-2 v1.1", profiles);
    }

    [Fact]
    public async Task DiscoverReportProfilesAreNotUserDefined()
    {
        await Client.PostAsJsonAsync(
            "/api/report/discover",
            JsonDocument.Parse(ReportJson).RootElement);

        var anyUserDefined = await Factory.QueryAsync(ctx =>
            ctx.SbomProfiles.AnyAsync(x => x.IsUserDefined));

        Assert.False(anyUserDefined);
    }

    [Fact]
    public async Task DiscoverReportCalledTwiceDoesNotDuplicateFeatures()
    {
        await Client.PostAsJsonAsync(
            "/api/report/discover",
            JsonDocument.Parse(ReportJson).RootElement);

        await Client.PostAsJsonAsync(
            "/api/report/discover",
            JsonDocument.Parse(ReportJson).RootElement);

        var featureCount = await Factory.QueryAsync(ctx =>
            ctx.SbomFeatures.CountAsync());

        Assert.Equal(4, featureCount);
    }

    [Fact]
    public async Task DiscoverReportCalledTwiceDoesNotDuplicateProfiles()
    {
        await Client.PostAsJsonAsync(
            "/api/report/discover",
            JsonDocument.Parse(ReportJson).RootElement);

        await Client.PostAsJsonAsync(
            "/api/report/discover",
            JsonDocument.Parse(ReportJson).RootElement);

        var profileCount = await Factory.QueryAsync(ctx =>
            ctx.SbomProfiles.CountAsync());

        Assert.Equal(3, profileCount);
    }

    [Fact]
    public async Task DiscoverReportInvalidJsonReturns400()
    {
        var content = new StringContent(
            "not-json",
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await Client.PostAsync("/api/report/discover", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
