using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.UnitTests.Fakes;

public class FakeSbomProfileRepository : ISbomProfileRepository
{
    private readonly HashSet<string> _existing;

    public List<SbomProfile> AddedProfiles { get; } = [];
    public bool AddRangeCalled { get; private set; }

    public FakeSbomProfileRepository(params string[] existingProfiles)
    {
        _existing = new HashSet<string>(existingProfiles, StringComparer.OrdinalIgnoreCase);
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