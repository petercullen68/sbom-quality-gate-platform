using System.Collections.Concurrent;
using NJsonSchema;

namespace SbomQualityGate.Infrastructure.Validation;

public sealed class SchemaCache
{
    private readonly record struct CacheEntry(JsonSchema Schema, DateTime FetchedAt);

    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly TimeSpan _ttl = TimeSpan.FromHours(24);

    public bool TryGet(string schemaUrl, out JsonSchema? schema, out DateTime fetchedAt)
    {
        if (_cache.TryGetValue(schemaUrl, out var entry) &&
            DateTime.UtcNow - entry.FetchedAt < _ttl)
        {
            schema = entry.Schema;
            fetchedAt = entry.FetchedAt;
            return true;
        }

        schema = null;
        fetchedAt = default;
        return false;
    }

    public void Set(string schemaUrl, JsonSchema schema, DateTime fetchedAt)
    {
        _cache[schemaUrl] = new CacheEntry(schema, fetchedAt);
    }
}
