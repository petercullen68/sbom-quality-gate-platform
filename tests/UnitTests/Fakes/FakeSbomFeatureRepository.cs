using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.UnitTests.Fakes;

public class FakeSbomFeatureRepository(params string[] existingFeatures) : ISbomFeatureRepository
{
    private readonly HashSet<string> _existing = new(existingFeatures, StringComparer.OrdinalIgnoreCase);

    public List<SbomFeature> AddedFeatures { get; } = [];
    public bool AddRangeCalled { get; private set; }

    public Task<List<string>> GetExistingFeaturesAsync(
        IEnumerable<string> features,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var matches = features
            .Where(f => _existing.Contains(f))
            .ToList();

        return Task.FromResult(matches);
    }

    public Task AddRangeAsync(
        IEnumerable<SbomFeature> features,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AddRangeCalled = true;
        AddedFeatures.AddRange(features);
        return Task.CompletedTask;
    }
}
