using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SbomQualityGate.Application.UseCases;

namespace SbomQualityGate.IntegrationTests.Tests;

public class SubmitSbomTests : IntegrationTestBase
{
    private static readonly string ReportJson = """
        {
          "files": [
            {
              "comprehenssive": [
                { "category": "Identification", "feature": "comp_with_name", "score": 10, "ignored": false }
              ],
              "profiles": [
                { "profile": "Interlynk", "score": 5.82, "grade": "D", "message": "Interlynk Scoring Profile" }
              ]
            }
          ]
        }
        """;

    private static readonly string CycloneDxSbom = """
        {
            "bomFormat": "CycloneDX",
            "specVersion": "1.5",
            "components": []
        }
        """;

    private static readonly string SpdxSbom = """
        {
            "spdxVersion": "SPDX-2.3",
            "packages": []
        }
        """;

    private Guid _productId;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        // Discover profiles first — required before SBOM submission
        await Client.PostAsJsonAsync(
            "/api/report/discover",
            JsonDocument.Parse(ReportJson).RootElement);

        // Get the seeded default product ID
        _productId = await Factory.QueryAsync(ctx =>
            ctx.Products.Select(x => x.Id).FirstAsync());
    }

    // ----------------------------------------------------------------
    // POST /api/sboms (JSON body)
    // ----------------------------------------------------------------

    [Fact]
    public async Task PostJsonCycloneDxReturns201()
    {
        var command = new SubmitSbomCommand
        {
            ProductId = _productId,
            Version = "1.0.0",
            SbomJson = CycloneDxSbom
        };

        var response = await Client.PostAsJsonAsync("/api/sboms", command);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PostJsonSpdxReturns201()
    {
        var command = new SubmitSbomCommand
        {
            ProductId = _productId,
            Version = "1.0.0",
            SbomJson = SpdxSbom
        };

        var response = await Client.PostAsJsonAsync("/api/sboms", command);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task PostJsonPersistsSbomAndCreatesJob()
    {
        var command = new SubmitSbomCommand
        {
            ProductId = _productId,
            Version = "1.0.0",
            SbomJson = CycloneDxSbom
        };

        var response = await Client.PostAsJsonAsync("/api/sboms", command);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var sbomId = body.GetProperty("id").GetGuid();

        var sbom = await Factory.QueryAsync(ctx =>
            ctx.Sboms.FirstOrDefaultAsync(x => x.Id == sbomId));

        Assert.NotNull(sbom);
        Assert.Equal(_productId, sbom.ProductId);
        Assert.Equal("1.0.0", sbom.Version);
        Assert.Equal("CycloneDX", sbom.SpecType);

        var job = await Factory.QueryAsync(ctx =>
            ctx.ValidationJobs.FirstOrDefaultAsync(x => x.SbomId == sbomId));

        Assert.NotNull(job);
    }

    [Fact]
    public async Task PostJsonInvalidJsonReturns400()
    {
        var command = new SubmitSbomCommand
        {
            ProductId = _productId,
            Version = "1.0.0",
            SbomJson = "not-valid-json"
        };

        var response = await Client.PostAsJsonAsync("/api/sboms", command);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostJsonUnknownProductReturns400()
    {
        var command = new SubmitSbomCommand
        {
            ProductId = Guid.NewGuid(),
            Version = "1.0.0",
            SbomJson = CycloneDxSbom
        };

        var response = await Client.PostAsJsonAsync("/api/sboms", command);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ----------------------------------------------------------------
    // POST /api/sboms/upload (multipart form)
    // ----------------------------------------------------------------

    [Fact]
    public async Task UploadFileCycloneDxReturns201()
    {
        using var content = BuildMultipartContent(CycloneDxSbom, "sbom.json");

        var response = await Client.PostAsync("/api/sboms/upload", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task UploadFileSpdxReturns201()
    {
        using var content = BuildMultipartContent(SpdxSbom, "sbom.json");

        var response = await Client.PostAsync("/api/sboms/upload", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task UploadFilePersistsSbomAndCreatesJob()
    {
        using var content = BuildMultipartContent(CycloneDxSbom, "sbom.json");

        var response = await Client.PostAsync("/api/sboms/upload", content);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var sbomId = body.GetProperty("id").GetGuid();

        var sbom = await Factory.QueryAsync(ctx =>
            ctx.Sboms.FirstOrDefaultAsync(x => x.Id == sbomId));

        Assert.NotNull(sbom);
        Assert.Equal(_productId, sbom.ProductId);

        var job = await Factory.QueryAsync(ctx =>
            ctx.ValidationJobs.FirstOrDefaultAsync(x => x.SbomId == sbomId));

        Assert.NotNull(job);
    }

    [Fact]
    public async Task UploadFileNotJsonReturns400()
    {
        using var content = BuildMultipartContent("this is not json", "sbom.txt");

        var response = await Client.PostAsync("/api/sboms/upload", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadFileMissingFileReturns400()
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(_productId.ToString()), "ProductId");
        content.Add(new StringContent("1.0.0"), "Version");

        var response = await Client.PostAsync("/api/sboms/upload", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ----------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------

    private MultipartFormDataContent BuildMultipartContent(string sbomJson, string fileName)
    {
        var content = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(sbomJson));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        content.Add(fileContent, "File", fileName);

        content.Add(new StringContent(_productId.ToString()), "ProductId");
        content.Add(new StringContent("1.0.0"), "Version");

        return content;
    }
}
