using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.UnitTests.Fakes;

public class FakeSbomRepository : ISbomRepository
{
    public bool AddCalled { get; private set; }
    public Sbom? AddedSbom { get; private set; }
    
    public Func<Guid, Sbom?>? GetByIdFunc { get; set; }

    public Task AddAsync(Sbom sbom, CancellationToken cancellationToken)
    {
        AddCalled = true;
        AddedSbom = sbom;
        return Task.CompletedTask;
    }

    public Task<Sbom?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        if (GetByIdFunc != null)
        {
            return Task.FromResult(GetByIdFunc(id));
        }

        return Task.FromResult<Sbom?>(null);
    }
}
