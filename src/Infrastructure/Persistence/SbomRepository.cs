using Microsoft.EntityFrameworkCore;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.Infrastructure.Persistence;

public class SbomRepository(AppDbContext context) : ISbomRepository
{
    public Task AddAsync(Sbom sbom, CancellationToken cancellationToken)
    {
        context.Sboms.Add(sbom);
        return Task.CompletedTask;
    }
    
    public async Task<Sbom?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await context.Sboms
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
