using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.Infrastructure.Persistence;

public class InMemorySbomRepository : ISbomRepository
{
    private readonly List<Sbom> _store = new();

    public Task SaveAsync(Sbom sbom, CancellationToken cancellationToken)
    {
        _store.Add(sbom);
        return Task.CompletedTask;
    }
}