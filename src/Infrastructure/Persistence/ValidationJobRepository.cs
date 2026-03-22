using System.Data;
using Microsoft.EntityFrameworkCore;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;
using SbomQualityGate.Domain.Enums;

namespace SbomQualityGate.Infrastructure.Persistence;

public class ValidationJobRepository(AppDbContext context) : IValidationJobRepository
{
    public async Task CreateAsync(ValidationJob job, CancellationToken cancellationToken)
    {
        context.ValidationJobs.Add(job);
        await context.SaveChangesAsync(cancellationToken);

        // Send notification safely
        var conn = context.Database.GetDbConnection();

        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"NOTIFY validation_jobs, '{job.Id}'";

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<ValidationJob?> ClaimNextPendingAsync(CancellationToken cancellationToken)
    {
        var sql = """
                           UPDATE "ValidationJobs"
                           SET "Status" = @p0,
                               "StartedAt" = NOW()
                           WHERE "Id" = (
                               SELECT "Id"
                               FROM "ValidationJobs"
                               WHERE "Status" = @p1
                               ORDER BY "CreatedAt"
                               FOR UPDATE SKIP LOCKED
                               LIMIT 1
                           )
                           RETURNING *;
                           """;

        var results = await context.ValidationJobs
            .FromSqlRaw(sql,
                ValidationJobStatus.InProgress,
                ValidationJobStatus.Pending)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return results.FirstOrDefault();
    }

    public async Task UpdateAsync(ValidationJob job, CancellationToken cancellationToken)
    {
        context.ValidationJobs.Update(job);
        await context.SaveChangesAsync(cancellationToken);
    }
    
    public async Task CompleteJobAsync(
        ValidationJob job,
        ValidationResult result,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        context.ValidationResults.Add(result);

        job.Status = result.Status == ValidationStatus.Pass
            ? ValidationJobStatus.Completed
            : ValidationJobStatus.Failed;

        job.CompletedAt = DateTime.UtcNow;

        context.ValidationJobs.Update(job);

        await context.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}