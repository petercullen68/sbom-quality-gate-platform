using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.UnitTests.Fakes;

public class FakeSbomFeatureRepository : ISbomFeatureRepository
{
    private readonly HashSet<string> _existing;

    public List<SbomFeature> AddedFeatures { get; } = [];
    public bool AddRangeCalled { get; private set; }

    public FakeSbomFeatureRepository(params string[] existingFeatures)
    {
        _existing = new HashSet<string>(existingFeatures, StringComparer.OrdinalIgnoreCase);
    }

    public Task<List<string>> GetExistingFeaturesAsync(
        IEnumerable<string> features,
        CancellationToken cancellationToken)
    {
        var matches = features
            .Where(f => _existing.Contains(f))
            .ToList();

        return Task.FromResult(matches);
    }

    public Task AddRangeAsync(
        IEnumerable<SbomFeature> features,
        CancellationToken cancellationToken)
    {
        AddRangeCalled = true;
        AddedFeatures.AddRange(features);
        return Task.CompletedTask;
    }
}
