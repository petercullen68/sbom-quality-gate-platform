using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.Application.Interfaces;

public interface ISbomProfileRepository
{
    Task<List<string>> GetExistingProfilesAsync(
        IEnumerable<string> names,
        CancellationToken cancellationToken);

    Task AddRangeAsync(
        IEnumerable<SbomProfile> profiles,
        CancellationToken cancellationToken);
}
