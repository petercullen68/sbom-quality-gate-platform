namespace SbomQualityGate.Application.Interfaces;

using SbomQualityGate.Domain.Entities;

public interface ISbomRepository
{
    Task SaveAsync(Sbom sbom, CancellationToken cancellationToken);
}