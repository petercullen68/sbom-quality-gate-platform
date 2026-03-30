using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.Application.Interfaces;

public interface ITeamRepository
{
    Task AddAsync(Team team, CancellationToken cancellationToken);
    Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
