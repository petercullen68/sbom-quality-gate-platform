using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.UnitTests.Fakes;

public class FakeSbomProfileRepository(
    bool anySystemProfilesExist = true,
    params string[] existingProfiles)
    : ISbomProfileRepository
{
    private readonly HashSet<string> _existing = new(existingProfiles, StringComparer.OrdinalIgnoreCase);

    public List<SbomProfile> AddedProfiles { get; } = [];
    public bool AddRangeCalled { get; private set; }

    public Task<bool> AnySystemProfilesExistAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(anySystemProfilesExist);
    }

    public Task<List<string>> GetExistingProfilesAsync(
        IEnumerable<string> names,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var matches = names
            .Where(n => _existing.Contains(n))
            .ToList();

        return Task.FromResult(matches);
    }

    public Task AddRangeAsync(
        IEnumerable<SbomProfile> profiles,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AddRangeCalled = true;
        AddedProfiles.AddRange(profiles);
        return Task.CompletedTask;
    }
}
