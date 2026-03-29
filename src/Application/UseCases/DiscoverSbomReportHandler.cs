using System.Text.Json;
using SbomQualityGate.Application.Exceptions;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.Application.UseCases;

public class DiscoverSbomReportHandler(
    ISbomFeatureRepository featureRepository,
    ISbomProfileRepository profileRepository,
    IUnitOfWork unitOfWork)
{
    public async Task HandleAsync(string reportJson, CancellationToken cancellationToken)
    {
        JsonDocument doc;

        try
        {
            doc = JsonDocument.Parse(reportJson);
        }
        catch (JsonException)
        {
            throw new RequestValidationException("Invalid JSON payload.");
        }

        using (doc)
        {
            var root = doc.RootElement;

            if (!root.TryGetProperty("files", out var files) ||
                files.ValueKind != JsonValueKind.Array)
            {
                throw new RequestValidationException("Missing or invalid 'files' array.");
            }

            var newFeatures = await DiscoverFeaturesAsync(files, cancellationToken);
            var newProfiles = await DiscoverProfilesAsync(files, cancellationToken);

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
    }

    private async Task<List<SbomFeature>> DiscoverFeaturesAsync(
        JsonElement files,
        CancellationToken cancellationToken)
    {
        var incoming = new Dictionary<string, (string Category, bool Ignored)>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var file in files.EnumerateArray())
        {
            if (!TryGetComprehensive(file, out var comprehensive))
                continue;

            foreach (var item in comprehensive.EnumerateArray())
            {
                if (!item.TryGetProperty("feature", out var featureProp) ||
                    featureProp.ValueKind != JsonValueKind.String)
                    continue;

                var feature = featureProp.GetString();
                if (string.IsNullOrWhiteSpace(feature))
                    continue;

                var category = item.TryGetProperty("category", out var catProp) &&
                               catProp.ValueKind == JsonValueKind.String
                    ? catProp.GetString()!
                    : string.Empty;

                var ignored = item.TryGetProperty("ignored", out var ignProp) &&
                              ignProp.ValueKind == JsonValueKind.True;

                incoming[feature] = (category, ignored);
            }
        }

        if (incoming.Count == 0)
            return [];

        var existing = await featureRepository.GetExistingFeaturesAsync(
            incoming.Keys,
            cancellationToken);

        var existingSet = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

        return incoming
            .Where(x => !existingSet.Contains(x.Key))
            .Select(x => new SbomFeature
            {
                Id = Guid.NewGuid(),
                Feature = x.Key,
                Category = x.Value.Category,
                Ignored = x.Value.Ignored,
                DiscoveredAt = DateTime.UtcNow
            })
            .ToList();
    }

    private async Task<List<SbomProfile>> DiscoverProfilesAsync(
        JsonElement files,
        CancellationToken cancellationToken)
    {
        var incoming = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files.EnumerateArray())
        {
            if (!file.TryGetProperty("profiles", out var profiles) ||
                profiles.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var profile in profiles.EnumerateArray())
            {
                if (!profile.TryGetProperty("profile", out var nameProp) ||
                    nameProp.ValueKind != JsonValueKind.String)
                    continue;

                var name = nameProp.GetString();
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var message = profile.TryGetProperty("message", out var msgProp) &&
                              msgProp.ValueKind == JsonValueKind.String
                    ? msgProp.GetString()!
                    : string.Empty;

                incoming[name] = message;
            }
        }

        if (incoming.Count == 0)
            return [];

        var existing = await profileRepository.GetExistingProfilesAsync(
            incoming.Keys,
            cancellationToken);

        var existingSet = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);

        return incoming
            .Where(x => !existingSet.Contains(x.Key))
            .Select(x => new SbomProfile
            {
                Id = Guid.NewGuid(),
                Name = x.Key,
                Description = x.Value,
                IsUserDefined = false,
                DiscoveredAt = DateTime.UtcNow
            })
            .ToList();
    }

    private static bool TryGetComprehensive(JsonElement file, out JsonElement result)
    {
        // NOTE:
        // SBOMQS currently outputs "comprehenssive" (typo).
        // We support both spellings to avoid breaking ingestion
        // if/when the tool fixes this in future versions.
        if (file.TryGetProperty("comprehenssive", out result) &&
            result.ValueKind == JsonValueKind.Array)
            return true;

        if (file.TryGetProperty("comprehensive", out result) &&
            result.ValueKind == JsonValueKind.Array)
            return true;

        result = default;
        return false;
    }
}
