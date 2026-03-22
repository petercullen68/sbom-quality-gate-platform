using Microsoft.EntityFrameworkCore;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.Infrastructure.Persistence;

public class SbomRepository(AppDbContext context) : ISbomRepository
{
    public async Task SaveAsync(Sbom sbom, CancellationToken cancellationToken)
    {
        context.Sboms.Add(sbom);
        await context.SaveChangesAsync(cancellationToken);
    }
}