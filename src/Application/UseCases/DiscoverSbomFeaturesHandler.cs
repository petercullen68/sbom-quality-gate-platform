using System.Text.Json;
using SbomQualityGate.Application.Exceptions;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.Application.UseCases;

public class DiscoverSbomFeaturesHandler(
    ISbomFeatureRepository repository,
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
                return;

            var existing = await repository.GetExistingFeaturesAsync(
                incoming.Keys,
                cancellationToken);

            var existingSet = new HashSet<string>(
                existing,
                StringComparer.OrdinalIgnoreCase);

            var newFeatures = incoming
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

            if (newFeatures.Count == 0)
                return;

            await unitOfWork.ExecuteAsync(async () =>
            {
                await repository.AddRangeAsync(newFeatures, cancellationToken);
            }, cancellationToken);
        }
    }

    private static bool TryGetComprehensive(JsonElement file, out JsonElement result)
    {
        // NOTE:
        // SBOMQS currently outputs "comprehenssive" (typo).
        // We support both spellings to avoid breaking ingestion
        // if/when the tool fixes this in future versions.
        // handle misspelling
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
