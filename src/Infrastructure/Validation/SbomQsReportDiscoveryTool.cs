using System.Text.Json;
using SbomQualityGate.Application.Exceptions;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Application.Models;

namespace SbomQualityGate.Infrastructure.Validation;

public class SbomQsReportDiscoveryTool : IReportDiscoveryTool
{
    public ReportDiscoveryResult Discover(string reportJson)
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

            var features = DiscoverFeatures(files);
            var profiles = DiscoverProfiles(files);

            return new ReportDiscoveryResult
            {
                Features = features,
                Profiles = profiles
            };
        }
    }

    private static List<DiscoveredFeature> DiscoverFeatures(JsonElement files)
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

        return incoming
            .Select(x => new DiscoveredFeature
            {
                Feature = x.Key,
                Category = x.Value.Category,
                Ignored = x.Value.Ignored
            })
            .ToList();
    }

    private static List<DiscoveredProfile> DiscoverProfiles(JsonElement files)
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

        return incoming
            .Select(x => new DiscoveredProfile
            {
                Name = x.Key,
                Description = x.Value
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
