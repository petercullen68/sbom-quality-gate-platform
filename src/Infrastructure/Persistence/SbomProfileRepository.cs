using Microsoft.EntityFrameworkCore;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.Infrastructure.Persistence;

public class SbomProfileRepository(AppDbContext context) : ISbomProfileRepository
{
    public async Task<List<string>> GetExistingProfilesAsync(
        IEnumerable<string> names,
        CancellationToken cancellationToken)
    {
        var list = names.ToList();

        return await context.SbomProfiles
            .Where(x => list.Contains(x.Name))
            .Select(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task AddRangeAsync(
        IEnumerable<SbomProfile> profiles,
        CancellationToken cancellationToken)
    {
        context.SbomProfiles.AddRange(profiles);
        return Task.CompletedTask;
    }
}
