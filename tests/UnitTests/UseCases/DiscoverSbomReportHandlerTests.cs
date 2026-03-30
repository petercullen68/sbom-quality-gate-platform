using SbomQualityGate.Application.Exceptions;
using SbomQualityGate.Application.Models;
using SbomQualityGate.Application.UseCases;
using SbomQualityGate.UnitTests.Fakes;

namespace SbomQualityGate.UnitTests.UseCases;

public class DiscoverSbomReportHandlerTests
{
    // ----------------------------------------------------------------
    // Helpers
    // ----------------------------------------------------------------

    private static DiscoverSbomReportHandler CreateHandler(
        FakeReportDiscoveryTool? discoveryTool = null,
        FakeSbomFeatureRepository? featureRepo = null,
        FakeSbomProfileRepository? profileRepo = null,
        FakeUnitOfWork? unitOfWork = null)
    {
        discoveryTool ??= new FakeReportDiscoveryTool();
        featureRepo ??= new FakeSbomFeatureRepository();
        profileRepo ??= new FakeSbomProfileRepository();
        unitOfWork ??= new FakeUnitOfWork();

        return new DiscoverSbomReportHandler(discoveryTool, featureRepo, profileRepo, unitOfWork);
    }

    private static ReportDiscoveryResult BuildResult(
        (string Feature, string Category, bool Ignored)[] features,
        (string Name, string Description)[] profiles)
    {
        return new ReportDiscoveryResult
        {
            Features = features.Select(f => new DiscoveredFeature
            {
                Feature = f.Feature,
                Category = f.Category,
                Ignored = f.Ignored
            }).ToList(),
            Profiles = profiles.Select(p => new DiscoveredProfile
            {
                Name = p.Name,
                Description = p.Description
            }).ToList()
        };
    }

    // ----------------------------------------------------------------
    // Features - happy path
    // ----------------------------------------------------------------

    [Fact]
    public async Task HandleAsyncNewFeaturesArePersistedInTransaction()
    {
        var featureRepo = new FakeSbomFeatureRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tool = new FakeReportDiscoveryTool
        {
            ResultToReturn = BuildResult(
                [("SBOM-GQ-001", "quality", false), ("SBOM-GQ-002", "quality", false)],
                [])
        };

        var handler = CreateHandler(discoveryTool: tool, featureRepo: featureRepo, unitOfWork: unitOfWork);

        await handler.HandleAsync("{}", CancellationToken.None);

        Assert.True(tool.WasCalled);
        Assert.True(unitOfWork.Executed);
        Assert.True(featureRepo.AddRangeCalled);
        Assert.Equal(2, featureRepo.AddedFeatures.Count);
    }

    [Fact]
    public async Task HandleAsyncExistingFeaturesAreNotReAdded()
    {
        var featureRepo = new FakeSbomFeatureRepository("SBOM-GQ-001");
        var tool = new FakeReportDiscoveryTool
        {
            ResultToReturn = BuildResult(
                [("SBOM-GQ-001", "quality", false), ("SBOM-GQ-002", "quality", false)],
                [])
        };

        var handler = CreateHandler(discoveryTool: tool, featureRepo: featureRepo);

        await handler.HandleAsync("{}", CancellationToken.None);

        Assert.Single(featureRepo.AddedFeatures);
        Assert.Equal("SBOM-GQ-002", featureRepo.AddedFeatures[0].Feature);
    }

    [Fact]
    public async Task HandleAsyncFeatureCategoryAndIgnoredAreMappedCorrectly()
    {
        var featureRepo = new FakeSbomFeatureRepository();
        var tool = new FakeReportDiscoveryTool
        {
            ResultToReturn = BuildResult(
                [("SBOM-GQ-010", "security", true)],
                [])
        };

        var handler = CreateHandler(discoveryTool: tool, featureRepo: featureRepo);

        await handler.HandleAsync("{}", CancellationToken.None);

        var feature = Assert.Single(featureRepo.AddedFeatures);
        Assert.Equal("security", feature.Category);
        Assert.True(feature.Ignored);
    }

    // ----------------------------------------------------------------
    // Profiles - happy path
    // ----------------------------------------------------------------

    [Fact]
    public async Task HandleAsyncNewProfilesArePersistedInTransaction()
    {
        var profileRepo = new FakeSbomProfileRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tool = new FakeReportDiscoveryTool
        {
            ResultToReturn = BuildResult(
                [],
                [("NTIA Minimum Elements (2021)", "NTIA Profile"), ("BSI TR-03183-2 v1.1", "BSI Profile")])
        };

        var handler = CreateHandler(discoveryTool: tool, profileRepo: profileRepo, unitOfWork: unitOfWork);

        await handler.HandleAsync("{}", CancellationToken.None);

        Assert.True(unitOfWork.Executed);
        Assert.True(profileRepo.AddRangeCalled);
        Assert.Equal(2, profileRepo.AddedProfiles.Count);
    }

    [Fact]
    public async Task HandleAsyncProfilesAreMarkedAsNotUserDefined()
    {
        var profileRepo = new FakeSbomProfileRepository();
        var tool = new FakeReportDiscoveryTool
        {
            ResultToReturn = BuildResult(
                [],
                [("Interlynk", "Interlynk Scoring Profile")])
        };

        var handler = CreateHandler(discoveryTool: tool, profileRepo: profileRepo);

        await handler.HandleAsync("{}", CancellationToken.None);

        var profile = Assert.Single(profileRepo.AddedProfiles);
        Assert.False(profile.IsUserDefined);
        Assert.Equal("Interlynk Scoring Profile", profile.Description);
    }

    [Fact]
    public async Task HandleAsyncExistingProfilesAreNotReAdded()
    {
        var profileRepo = new FakeSbomProfileRepository(existingProfiles: "Interlynk");
        var tool = new FakeReportDiscoveryTool
        {
            ResultToReturn = BuildResult(
                [],
                [("Interlynk", "Interlynk Scoring Profile"), ("BSI TR-03183-2 v1.1", "BSI Profile")])
        };

        var handler = CreateHandler(discoveryTool: tool, profileRepo: profileRepo);

        await handler.HandleAsync("{}", CancellationToken.None);

        Assert.Single(profileRepo.AddedProfiles);
        Assert.Equal("BSI TR-03183-2 v1.1", profileRepo.AddedProfiles[0].Name);
    }

    // ----------------------------------------------------------------
    // Combined
    // ----------------------------------------------------------------

    [Fact]
    public async Task HandleAsyncFeaturesAndProfilesPersistedInSingleTransaction()
    {
        var featureRepo = new FakeSbomFeatureRepository();
        var profileRepo = new FakeSbomProfileRepository();
        var unitOfWork = new FakeUnitOfWork();
        var tool = new FakeReportDiscoveryTool
        {
            ResultToReturn = BuildResult(
                [("SBOM-GQ-001", "quality", false)],
                [("Interlynk", "Interlynk Scoring Profile")])
        };

        var handler = CreateHandler(tool, featureRepo, profileRepo, unitOfWork);

        await handler.HandleAsync("{}", CancellationToken.None);

        Assert.True(unitOfWork.Executed);
        Assert.Single(featureRepo.AddedFeatures);
        Assert.Single(profileRepo.AddedProfiles);
    }

    [Fact]
    public async Task HandleAsyncAllExistingNoTransactionExecuted()
    {
        var featureRepo = new FakeSbomFeatureRepository("SBOM-GQ-001");
        var profileRepo = new FakeSbomProfileRepository(existingProfiles: "Interlynk");
        var unitOfWork = new FakeUnitOfWork();
        var tool = new FakeReportDiscoveryTool
        {
            ResultToReturn = BuildResult(
                [("SBOM-GQ-001", "quality", false)],
                [("Interlynk", "Interlynk Scoring Profile")])
        };

        var handler = CreateHandler(tool, featureRepo, profileRepo, unitOfWork);

        await handler.HandleAsync("{}", CancellationToken.None);

        Assert.False(unitOfWork.Executed);
        Assert.False(featureRepo.AddRangeCalled);
        Assert.False(profileRepo.AddRangeCalled);
    }

    // ----------------------------------------------------------------
    // Error handling
    // ----------------------------------------------------------------

    [Fact]
    public async Task HandleAsyncDiscoveryToolThrowsRequestValidationException()
    {
        var tool = new FakeReportDiscoveryTool { ShouldThrow = true };
        var handler = CreateHandler(discoveryTool: tool);

        var ex = await Assert.ThrowsAsync<RequestValidationException>(() =>
            handler.HandleAsync("{}", CancellationToken.None));

        Assert.Contains("Simulated discovery tool failure", ex.Message);
    }

    [Fact]
    public async Task HandleAsyncCancellationTokenIsRespected()
    {
        var featureRepo = new FakeSbomFeatureRepository();
        var tool = new FakeReportDiscoveryTool
        {
            ResultToReturn = BuildResult(
                [("SBOM-GQ-001", "quality", false)],
                [])
        };

        var handler = CreateHandler(discoveryTool: tool, featureRepo: featureRepo);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            handler.HandleAsync("{}", cts.Token));
    }
}
