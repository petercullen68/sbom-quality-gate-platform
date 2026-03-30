using Microsoft.EntityFrameworkCore;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.Infrastructure.Persistence;

public class TeamRepository(AppDbContext context) : ITeamRepository
{
    public Task AddAsync(Team team, CancellationToken cancellationToken)
    {
        context.Teams.Add(team);
        return Task.CompletedTask;
    }

    public async Task<Team?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Teams
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
