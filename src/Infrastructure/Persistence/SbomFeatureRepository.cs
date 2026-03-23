using Microsoft.EntityFrameworkCore;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.Infrastructure.Persistence;

public class SbomFeatureRepository(AppDbContext context) : ISbomFeatureRepository
{
    public async Task<List<string>> GetExistingFeaturesAsync(
        IEnumerable<string> features,
        CancellationToken cancellationToken)
    {
        var list = features.ToList();

        return await context.SbomFeatures
            .Where(x => list.Contains(x.Feature))
            .Select(x => x.Feature)
            .ToListAsync(cancellationToken);
    }

    public Task AddRangeAsync(
        IEnumerable<SbomFeature> features,
        CancellationToken cancellationToken)
    {
        context.SbomFeatures.AddRange(features);
        return Task.CompletedTask;
    }
}
