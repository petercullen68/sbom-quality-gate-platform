using System.Collections.Concurrent;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.Infrastructure.Persistence;

public class InMemorySbomRepository : ISbomRepository
{
    private readonly ConcurrentDictionary<Guid, Sbom> _store = new();

    public Task SaveAsync(Sbom sbom, CancellationToken cancellationToken)
    {
        _store[sbom.Id] = sbom;
        return Task.CompletedTask;
    }

    public Task<Sbom?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        _store.TryGetValue(id, out var sbom);
        return Task.FromResult(sbom);
    }
}