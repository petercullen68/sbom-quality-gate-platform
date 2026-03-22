namespace SbomQualityGate.Application.Interfaces;

using Domain.Entities;

public interface ISbomRepository
{
    Task SaveAsync(Sbom sbom, CancellationToken cancellationToken);
}