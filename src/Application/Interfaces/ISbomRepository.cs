namespace SbomQualityGate.Application.Interfaces;

using Domain.Entities;

public interface ISbomRepository
{
    Task AddAsync(Sbom sbom, CancellationToken cancellationToken);
    
    Task<Sbom?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
