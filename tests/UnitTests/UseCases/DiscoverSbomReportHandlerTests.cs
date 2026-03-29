using System.Globalization;
using SbomQualityGate.Application.Exceptions;
using SbomQualityGate.Application.UseCases;
using SbomQualityGate.UnitTests.Fakes;

namespace SbomQualityGate.UnitTests.UseCases;

public class DiscoverSbomReportHandlerTests
{
    // ----------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------

    private static string BuildReport(
        string featureSpelling,
        (string Feature, string Category, bool Ignored)[] features,
        (string Profile, string Message)[] profiles)
    {
        var featuresJson = string.Join(",", features.Select(f =>
            $$"""{"feature":"{{f.Feature}}","category":"{{f.Category}}","ignored":{{f.Ignored.ToString().ToLower(CultureInfo.InvariantCulture)}}}"""));

        var profilesJson = string.Join(",", profiles.Select(p =>
            $$"""{"profile":"{{p.Profile}}","score":7.5,"grade":"C","message":"{{p.Message}}"}"""));

        return $$"""
                 {
                     "files": [
                         {
                             "{{featureSpelling}}": [{{featuresJson}}],
                             "profiles": [{{profilesJson}}]
                         }
                     ]
                 }
                 """;
    }

    private static DiscoverSbomReportHandler CreateHandler(
        FakeSbomFeatureRepository? featureRepo = null,
        FakeSbomProfileRepository? profileRepo = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        featureRepo ??= new FakeSbomFeatureRepository();
        profileRepo ??= new FakeSbomProfileRepository();
        unitOfWork ??= new FakeUnitOfWork();
        return new DiscoverSbomReportHandler(featureRepo, profileRepo, unitOfWork);
    }

    // ----------------------------------------------------------------
    // Features - happy path
    // ----------------------------------------------------------------

    [Fact]
    public async Task HandleAsyncNewFeaturesArePersistedInTransaction()
    {
        var featureRepo = new FakeSbomFeatureRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(featureRepo: featureRepo, unitOfWork: unitOfWork);

        var json = BuildReport("comprehenssive",
            [("SBOM-GQ-001", "quality", false), ("SBOM-GQ-002", "quality", false)],
            []);

        await handler.HandleAsync(json, CancellationToken.None);

        Assert.True(unitOfWork.Executed);
        Assert.True(featureRepo.AddRangeCalled);
        Assert.Equal(2, featureRepo.AddedFeatures.Count);
    }

    [Fact]
    public async Task HandleAsyncCorrectSpellingOfComprehensiveIsSupported()
    {
        var featureRepo = new FakeSbomFeatureRepository();
        var handler = CreateHandler(featureRepo: featureRepo);

        var json = BuildReport("comprehensive",
            [("SBOM-GQ-003", "licensing", false)],
            []);

        await handler.HandleAsync(json, CancellationToken.None);

        Assert.Single(featureRepo.AddedFeatures);
        Assert.Equal("SBOM-GQ-003", featureRepo.AddedFeatures[0].Feature);
    }

    [Fact]
    public async Task HandleAsyncExistingFeaturesAreNotReAdded()
    {
        var featureRepo = new FakeSbomFeatureRepository("SBOM-GQ-001");
        var handler = CreateHandler(featureRepo: featureRepo);

        var json = BuildReport("comprehenssive",
            [("SBOM-GQ-001", "quality", false), ("SBOM-GQ-002", "quality", false)],
            []);

        await handler.HandleAsync(json, CancellationToken.None);

        Assert.Single(featureRepo.AddedFeatures);
        Assert.Equal("SBOM-GQ-002", featureRepo.AddedFeatures[0].Feature);
    }

    // ----------------------------------------------------------------
    // Profiles - happy path
    // ----------------------------------------------------------------

    [Fact]
    public async Task HandleAsyncNewProfilesArePersistedInTransaction()
    {
        var profileRepo = new FakeSbomProfileRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(profileRepo: profileRepo, unitOfWork: unitOfWork);

        var json = BuildReport("comprehenssive",
            [],
            [("NTIA Minimum Elements (2021)", "NTIA Profile"), ("BSI TR-03183-2 v1.1", "BSI Profile")]);

        await handler.HandleAsync(json, CancellationToken.None);

        Assert.True(unitOfWork.Executed);
        Assert.True(profileRepo.AddRangeCalled);
        Assert.Equal(2, profileRepo.AddedProfiles.Count);
        Assert.Contains(profileRepo.AddedProfiles, p => p.Name == "NTIA Minimum Elements (2021)");
        Assert.Contains(profileRepo.AddedProfiles, p => p.Name == "BSI TR-03183-2 v1.1");
    }

    [Fact]
    public async Task HandleAsyncProfilesAreMarkedAsNotUserDefined()
    {
        var profileRepo = new FakeSbomProfileRepository();
        var handler = CreateHandler(profileRepo: profileRepo);

        var json = BuildReport("comprehenssive",
            [],
            [("Interlynk", "Interlynk Scoring Profile")]);

        await handler.HandleAsync(json, CancellationToken.None);

        var profile = Assert.Single(profileRepo.AddedProfiles);
        Assert.False(profile.IsUserDefined);
        Assert.Equal("Interlynk Scoring Profile", profile.Description);
    }

    [Fact]
    public async Task HandleAsyncExistingProfilesAreNotReAdded()
    {
        var profileRepo = new FakeSbomProfileRepository("Interlynk");
        var handler = CreateHandler(profileRepo: profileRepo);

        var json = BuildReport("comprehenssive",
            [],
            [("Interlynk", "Interlynk Scoring Profile"), ("BSI TR-03183-2 v1.1", "BSI Profile")]);

        await handler.HandleAsync(json, CancellationToken.None);

        Assert.Single(profileRepo.AddedProfiles);
        Assert.Equal("BSI TR-03183-2 v1.1", profileRepo.AddedProfiles[0].Name);
    }

    [Fact]
    public async Task HandleAsyncDuplicateProfilesInSamePayloadAreDeduped()
    {
        var profileRepo = new FakeSbomProfileRepository();
        var handler = CreateHandler(profileRepo: profileRepo);

        var json = BuildReport("comprehenssive",
            [],
            [("Interlynk", "Interlynk Scoring Profile"), ("Interlynk", "Interlynk Scoring Profile")]);

        await handler.HandleAsync(json, CancellationToken.None);

        Assert.Single(profileRepo.AddedProfiles);
    }

    // ----------------------------------------------------------------
    // Combined - features and profiles together
    // ----------------------------------------------------------------

    [Fact]
    public async Task HandleAsyncFeaturesAndProfilesPersistedInSingleTransaction()
    {
        var featureRepo = new FakeSbomFeatureRepository();
        var profileRepo = new FakeSbomProfileRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(featureRepo, profileRepo, unitOfWork);

        var json = BuildReport("comprehenssive",
            [("SBOM-GQ-001", "quality", false)],
            [("Interlynk", "Interlynk Scoring Profile")]);

        await handler.HandleAsync(json, CancellationToken.None);

        Assert.True(unitOfWork.Executed);
        Assert.Single(featureRepo.AddedFeatures);
        Assert.Single(profileRepo.AddedProfiles);
    }

    [Fact]
    public async Task HandleAsyncAllExistingNoTransactionExecuted()
    {
        var featureRepo = new FakeSbomFeatureRepository("SBOM-GQ-001");
        var profileRepo = new FakeSbomProfileRepository("Interlynk");
        var unitOfWork = new FakeUnitOfWork();
        var handler = CreateHandler(featureRepo, profileRepo, unitOfWork);

        var json = BuildReport("comprehenssive",
            [("SBOM-GQ-001", "quality", false)],
            [("Interlynk", "Interlynk Scoring Profile")]);

        await handler.HandleAsync(json, CancellationToken.None);

        Assert.False(unitOfWork.Executed);
        Assert.False(featureRepo.AddRangeCalled);
        Assert.False(profileRepo.AddRangeCalled);
    }

    // ----------------------------------------------------------------
    // Error handling
    // ----------------------------------------------------------------

    [Fact]
    public async Task HandleAsyncInvalidJsonThrowsRequestValidationException()
    {
        var handler = CreateHandler();

        var ex = await Assert.ThrowsAsync<RequestValidationException>(() =>
            handler.HandleAsync("not-json", CancellationToken.None));

        Assert.Contains("Invalid JSON payload", ex.Message);
    }

    [Fact]
    public async Task HandleAsyncMissingFilesArrayThrowsRequestValidationException()
    {
        var handler = CreateHandler();

        var ex = await Assert.ThrowsAsync<RequestValidationException>(() =>
            handler.HandleAsync("""{ "other": [] }""", CancellationToken.None));

        Assert.Contains("files", ex.Message);
    }

    [Fact]
    public async Task HandleAsyncCancellationTokenIsRespected()
    {
        var handler = CreateHandler();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var json = BuildReport("comprehenssive",
            [("SBOM-GQ-001", "quality", false)],
            [("Interlynk", "Interlynk Scoring Profile")]);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            handler.HandleAsync(json, cts.Token));
    }
}
