using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Application.Models;
using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.Application.UseCases;

public class DiscoverSbomReportHandler(
    IReportDiscoveryTool discoveryTool,
    ISbomFeatureRepository featureRepository,
    ISbomProfileRepository profileRepository,
    IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(string reportJson, CancellationToken cancellationToken)
    {
        var discovered = discoveryTool.Discover(reportJson);

        var newFeatures = await ResolveNewFeaturesAsync(discovered.Features, cancellationToken);
        var newProfiles = await ResolveNewProfilesAsync(discovered.Profiles, cancellationToken);

        if (newFeatures.Count == 0 && newProfiles.Count == 0)
            return;

        await unitOfWork.ExecuteAsync(async () =>
        {
            if (newFeatures.Count > 0)
                await featureRepository.AddRangeAsync(newFeatures, cancellationToken);

            if (newProfiles.Count > 0)
                await profileRepository.AddRangeAsync(newProfiles, cancellationToken);

        }, cancellationToken);
    }

    private async Task<List<SbomFeature>> ResolveNewFeaturesAsync(
        IReadOnlyList<DiscoveredFeature> features,
        CancellationToken cancellationToken)
    {
        if (features.Count == 0)
            return [];

        var existing = await featureRepository.GetExistingFeaturesAsync(
            features.Select(x => x.Feature),
            cancellationToken);

        var existingSet = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

        return features
            .Where(x => !existingSet.Contains(x.Feature))
            .Select(x => new SbomFeature
            {
                Id = Guid.NewGuid(),
                Feature = x.Feature,
                Category = x.Category,
                Ignored = x.Ignored,
                DiscoveredAt = DateTime.UtcNow
            })
            .ToList();
    }

    private async Task<List<SbomProfile>> ResolveNewProfilesAsync(
        IReadOnlyList<DiscoveredProfile> profiles,
        CancellationToken cancellationToken)
    {
        if (profiles.Count == 0)
            return [];

        var existing = await profileRepository.GetExistingProfilesAsync(
            profiles.Select(x => x.Name),
            cancellationToken);

        var existingSet = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

        return profiles
            .Where(x => !existingSet.Contains(x.Name))
            .Select(x => new SbomProfile
            {
                Id = Guid.NewGuid(),
                Name = x.Name,
                Description = x.Description,
                IsUserDefined = false,
                DiscoveredAt = DateTime.UtcNow
            })
            .ToList();
    }
}
