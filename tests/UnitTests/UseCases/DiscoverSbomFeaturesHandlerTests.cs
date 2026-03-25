using System.Globalization;
using SbomQualityGate.Application.UseCases;
using SbomQualityGate.UnitTests.Fakes;

namespace SbomQualityGate.UnitTests.UseCases;

public class DiscoverSbomFeaturesHandlerTests
{
    // ----------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------

    private static string BuildReport(string spelling, params (string Feature, string Category, bool Ignored)[] items)
    {
        var itemsJson = string.Join(",", items.Select(i =>
            $$"""{"feature":"{{i.Feature}}","category":"{{i.Category}}","ignored":{{i.Ignored.ToString().ToLower(CultureInfo.InvariantCulture)}}}"""));

        return $$"""
                 {
                     "files": [
                         {
                             "{{spelling}}": [{{itemsJson}}]
                         }
                     ]
                 }
                 """;
    }

    private static DiscoverSbomFeaturesHandler CreateHandler(
        FakeSbomFeatureRepository? repo = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        repo ??= new FakeSbomFeatureRepository();
        unitOfWork ??= new FakeUnitOfWork();
        return new DiscoverSbomFeaturesHandler(repo, unitOfWork);
    }

    // ----------------------------------------------------------------
    // Happy path
    // ----------------------------------------------------------------

    [Fact]
    public async Task HandleAsyncNewFeaturesArePersistedAndTransactionExecuted()
    {
        // Arrange
        var repo = new FakeSbomFeatureRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(repo, unitOfWork);

        var json = BuildReport("comprehenssive",
            ("SBOM-GQ-001", "quality", false),
            ("SBOM-GQ-002", "quality", false));

        // Act
        await handler.HandleAsync(json, CancellationToken.None);

        // Assert
        Assert.True(unitOfWork.Executed);
        Assert.True(repo.AddRangeCalled);
        Assert.Equal(2, repo.AddedFeatures.Count);
        Assert.Contains(repo.AddedFeatures, f => f.Feature == "SBOM-GQ-001");
        Assert.Contains(repo.AddedFeatures, f => f.Feature == "SBOM-GQ-002");
    }

    [Fact]
    public async Task HandleAsyncCorrectSpellingOfComprehensiveIsAlsoSupported()
    {
        // Arrange
        var repo = new FakeSbomFeatureRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(repo, unitOfWork);

        var json = BuildReport("comprehensive",
            ("SBOM-GQ-003", "licensing", false));

        // Act
        await handler.HandleAsync(json, CancellationToken.None);

        // Assert
        Assert.True(repo.AddRangeCalled);
        Assert.Single(repo.AddedFeatures);
        Assert.Equal("SBOM-GQ-003", repo.AddedFeatures[0].Feature);
    }

    [Fact]
    public async Task HandleAsyncCategoryAndIgnoredAreMappedCorrectly()
    {
        // Arrange
        var repo = new FakeSbomFeatureRepository();
        var handler = CreateHandler(repo);

        var json = BuildReport("comprehenssive",
            ("SBOM-GQ-010", "security", true));

        // Act
        await handler.HandleAsync(json, CancellationToken.None);

        // Assert
        var feature = Assert.Single(repo.AddedFeatures);
        Assert.Equal("security", feature.Category);
        Assert.True(feature.Ignored);
    }

    // ----------------------------------------------------------------
    // Deduplication
    // ----------------------------------------------------------------

    [Fact]
    public async Task HandleAsyncExistingFeaturesAreNotReAdded()
    {
        // Arrange
        var repo = new FakeSbomFeatureRepository("SBOM-GQ-001");
        var handler = CreateHandler(repo);

        var json = BuildReport("comprehenssive",
            ("SBOM-GQ-001", "quality", false),
            ("SBOM-GQ-002", "quality", false));

        // Act
        await handler.HandleAsync(json, CancellationToken.None);

        // Assert
        Assert.Single(repo.AddedFeatures);
        Assert.Equal("SBOM-GQ-002", repo.AddedFeatures[0].Feature);
    }

    [Fact]
    public async Task HandleAsyncDuplicateFeaturesInSamePayloadAreDeduped()
    {
        // Arrange
        var repo = new FakeSbomFeatureRepository();
        var handler = CreateHandler(repo);

        var json = BuildReport("comprehenssive",
            ("SBOM-GQ-001", "quality", false),
            ("SBOM-GQ-001", "quality", false));

        // Act
        await handler.HandleAsync(json, CancellationToken.None);

        // Assert
        Assert.Single(repo.AddedFeatures);
    }

    [Fact]
    public async Task HandleAsyncAllFeaturesAlreadyExistDoesNotCallAddRange()
    {
        // Arrange
        var repo = new FakeSbomFeatureRepository("SBOM-GQ-001", "SBOM-GQ-002");
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(repo, unitOfWork);

        var json = BuildReport("comprehenssive",
            ("SBOM-GQ-001", "quality", false),
            ("SBOM-GQ-002", "quality", false));

        // Act
        await handler.HandleAsync(json, CancellationToken.None);

        // Assert
        Assert.False(repo.AddRangeCalled);
        Assert.False(unitOfWork.Executed);
    }

    // ----------------------------------------------------------------
    // Edge cases / graceful degradation
    // ----------------------------------------------------------------

    [Fact]
    public async Task HandleAsyncEmptyFilesArrayDoesNotThrow()
    {
        // Arrange
        var repo = new FakeSbomFeatureRepository();
        var handler = CreateHandler(repo);

        var json = """{ "files": [] }""";

        // Act
        await handler.HandleAsync(json, CancellationToken.None);

        // Assert
        Assert.False(repo.AddRangeCalled);
    }

    [Fact]
    public async Task HandleAsyncFileWithNoComprehensivePropertyIsSkipped()
    {
        // Arrange
        var repo = new FakeSbomFeatureRepository();
        var handler = CreateHandler(repo);

        var json = """{ "files": [{ "otherProp": [] }] }""";

        // Act
        await handler.HandleAsync(json, CancellationToken.None);

        // Assert
        Assert.False(repo.AddRangeCalled);
    }

    [Fact]
    public async Task HandleAsyncItemsMissingFeaturePropertyAreSkipped()
    {
        // Arrange
        var repo = new FakeSbomFeatureRepository();
        var handler = CreateHandler(repo);

        var json = """
                   {
                       "files": [
                           {
                               "comprehenssive": [
                                   { "category": "quality", "ignored": false }
                               ]
                           }
                       ]
                   }
                   """;

        // Act
        await handler.HandleAsync(json, CancellationToken.None);

        // Assert
        Assert.False(repo.AddRangeCalled);
    }

    // ----------------------------------------------------------------
    // Error handling
    // ----------------------------------------------------------------

    [Fact]
    public async Task HandleAsyncInvalidJsonThrowsInvalidOperationException()
    {
        // Arrange
        var handler = CreateHandler();

        // Act + Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync("not-json", CancellationToken.None));

        Assert.Contains("Invalid JSON payload", ex.Message);
    }

    [Fact]
    public async Task HandleAsyncMissingFilesArrayThrowsInvalidOperationException()
    {
        // Arrange
        var handler = CreateHandler();

        // Act + Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync("""{ "other": [] }""", CancellationToken.None));

        Assert.Contains("files", ex.Message);
    }

    [Fact]
    public async Task HandleAsyncCancellationTokenIsRespected()
    {
        // Arrange
        var handler = CreateHandler();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var json = BuildReport("comprehenssive", ("SBOM-GQ-001", "quality", false));

        // Act + Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            handler.HandleAsync(json, cts.Token));
    }
}
