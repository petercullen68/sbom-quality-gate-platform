using Microsoft.Extensions.Options;
using NJsonSchema;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Application.Models;
using SbomQualityGate.Domain.Enums;

namespace SbomQualityGate.Infrastructure.Validation;

public class SpecConformanceTool(
    SchemaCache schemaCache,
    IOptions<SpecSchemaOptions> schemaOptions) : ISpecConformanceTool
{
    private readonly SpecSchemaOptions _options = schemaOptions.Value;
    
    public async Task<SpecConformanceResult> CheckAsync(
        string sbomJson,
        string specType,
        string specVersion,
        CancellationToken cancellationToken)
    {
        var schemaUrl = ResolveSchemaUrl(specType, specVersion);

        if (schemaUrl is null)
        {
            // Unknown spec type — not just a missing schema URL
            if (!IsKnownSpecType(specType))
            {
                return new SpecConformanceResult
                {
                    Status = SpecConformanceStatus.NonConformant,
                    Violations = [$"No schema available for unknown spec type '{specType}'"],
                    SchemaUrl = string.Empty,
                    FetchedAt = DateTime.UtcNow
                };
            }

            // Known spec type but version not configured — skip gracefully
            return new SpecConformanceResult
            {
                Status = SpecConformanceStatus.Skipped,
                Violations = [],
                SchemaUrl = string.Empty,
                FetchedAt = DateTime.UtcNow
            };
        }

        JsonSchema schema;
        DateTime fetchedAt;

        try
        {
            (schema, fetchedAt) = await GetSchemaAsync(schemaUrl, cancellationToken);
        }
        catch (Exception)
        {
            // Network or parse error fetching schema — skip conformance
            // rather than failing the job
            return new SpecConformanceResult
            {
                Status = SpecConformanceStatus.Skipped,
                Violations = [],
                SchemaUrl = schemaUrl,
                FetchedAt = DateTime.UtcNow
            };
        }

        Newtonsoft.Json.Linq.JToken sbomToken;
        try
        {
            sbomToken = Newtonsoft.Json.Linq.JToken.Parse(sbomJson);
        }
        catch (Exception ex)
        {
            return new SpecConformanceResult
            {
                Status = SpecConformanceStatus.NonConformant,
                Violations = [$"SBOM JSON could not be parsed: {ex.Message}"],
                SchemaUrl = schemaUrl,
                FetchedAt = fetchedAt
            };
        }

        var validationErrors = schema.Validate(sbomToken);
        var violations = validationErrors
            .Select(e => $"{e.Path}: {e.Kind}")
            .ToList();

        var deprecationWarnings = FindDeprecatedFieldsInUse(schema, sbomToken);

        return new SpecConformanceResult
        {
            Status = violations.Count == 0
                ? SpecConformanceStatus.Conformant
                : SpecConformanceStatus.NonConformant,
            Violations = violations,
            DeprecationWarnings = deprecationWarnings,
            SchemaUrl = schemaUrl,
            FetchedAt = fetchedAt
        };
    }
    
    private static bool IsKnownSpecType(string specType) =>
        specType is "CycloneDX" or "SPDX";

    private async Task<(JsonSchema Schema, DateTime FetchedAt)> GetSchemaAsync(
        string schemaUrl,
        CancellationToken cancellationToken)
    {
        if (schemaCache.TryGet(schemaUrl, out var cached, out var cachedAt))
            return (cached!, cachedAt);

        // Use FromUrlAsync so NJsonSchema can resolve relative $ref paths
        // (e.g. jsf-0.82.schema.json referenced by CycloneDX schemas)
        var schema = await JsonSchema.FromUrlAsync(schemaUrl, cancellationToken);
        var fetchedAt = DateTime.UtcNow;

        schemaCache.Set(schemaUrl, schema, fetchedAt);

        return (schema, fetchedAt);
    }

    private string? ResolveSchemaUrl(string specType, string specVersion)
    {
        // SPDX specVersion is stored as "SPDX-2.3" — normalise to "2.3"
        var normalizedVersion = specType == "SPDX" &&
                                specVersion.StartsWith("SPDX-", StringComparison.OrdinalIgnoreCase)
            ? specVersion["SPDX-".Length..]
            : specVersion;

        var lookup = specType switch
        {
            "CycloneDX" => _options.CycloneDx,
            "SPDX" => _options.Spdx,
            _ => null
        };

        if (lookup is null)
            return null;

        return lookup.GetValueOrDefault(normalizedVersion);
    }

    private static List<string> FindDeprecatedFieldsInUse(
        JsonSchema schema,
        Newtonsoft.Json.Linq.JToken sbomToken)
    {
        var warnings = new List<string>();
        WalkSchema(schema, sbomToken, string.Empty, warnings, []);
        return warnings;
    }
    
    private static bool IsDeprecated(JsonSchema schema)
    {
        // NJsonSchema.IsDeprecated only reads x-deprecated (OpenAPI extension).
        // CycloneDX and SPDX schemas use the standard "deprecated": true keyword
        // from JSON Schema draft 2019-09+, which NJsonSchema stores in ExtensionData.
        if (schema.IsDeprecated)
            return true;

        if (schema.ExtensionData != null &&
            schema.ExtensionData.TryGetValue("deprecated", out var value))
        {
            return value is true or "true";
        }

        return false;
    }

    private static void WalkSchema(
        JsonSchema schema,
        Newtonsoft.Json.Linq.JToken? token,
        string path,
        List<string> warnings,
        HashSet<JsonSchema> visited)
    {
        // Guard against circular $ref chains
        if (!visited.Add(schema))
            return;

        if (schema.Properties is not { Count: > 0 })
            return;

        foreach (var (propertyName, propertySchema) in schema.Properties)
        {
            var propertyPath = string.IsNullOrEmpty(path)
                ? propertyName
                : $"{path}.{propertyName}";

            // Resolve $ref if present
            var resolvedSchema = propertySchema.Reference ?? propertySchema;

            var propertyToken = token is Newtonsoft.Json.Linq.JObject obj
                ? obj[propertyName]
                : null;

            if (propertyToken is not null and not Newtonsoft.Json.Linq.JValue { Value: null })
            {
                if (IsDeprecated(resolvedSchema))
                {
                    warnings.Add($"{propertyPath} is deprecated");
                }

                // Recurse into objects
                if (propertyToken is Newtonsoft.Json.Linq.JObject)
                {
                    WalkSchema(resolvedSchema, propertyToken, propertyPath, warnings, visited);
                }

                // Recurse into array items
                if (propertyToken is Newtonsoft.Json.Linq.JArray array &&
                    resolvedSchema.Item != null)
                {
                    var itemSchema = resolvedSchema.Item.Reference ?? resolvedSchema.Item;
                    foreach (var item in array)
                    {
                        WalkSchema(itemSchema, item, propertyPath + "[]", warnings, [..visited]);
                    }
                }
            }
        }
    }
}
