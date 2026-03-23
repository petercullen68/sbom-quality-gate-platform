using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.Application.Interfaces;

public interface ISbomFeatureRepository
{
    Task<List<string>> GetExistingFeaturesAsync(
        IEnumerable<string> features,
        CancellationToken cancellationToken);

    Task AddRangeAsync(
        IEnumerable<SbomFeature> features,
        CancellationToken cancellationToken);
}
