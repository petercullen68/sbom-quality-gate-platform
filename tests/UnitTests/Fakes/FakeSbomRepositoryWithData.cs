using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.UnitTests.Fakes;

public class FakeSbomRepositoryWithData : ISbomRepository
{
    public Task AddAsync(Sbom sbom, CancellationToken cancellationToken)
    {
        return Task.FromResult<Sbom?>(null);
    }

    public Task<Sbom?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult<Sbom?>(new Sbom
        {
            Id = id,
            SbomJson = "{}"
        });
    }
}
