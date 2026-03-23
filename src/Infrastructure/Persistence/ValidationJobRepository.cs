using Microsoft.EntityFrameworkCore;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;
using SbomQualityGate.Domain.Enums;

namespace SbomQualityGate.Infrastructure.Persistence;

public class ValidationJobRepository(AppDbContext context) : IValidationJobRepository
{
    public Task AddAsync(ValidationJob job, CancellationToken cancellationToken)
    {
        context.ValidationJobs.Add(job);
        return Task.CompletedTask;
    }

    public Task FailJobAsync(
        ValidationJob job,
        string reason,
        CancellationToken cancellationToken)
    {
        job.RetryCount++;

        if (job.RetryCount >= 3)
        {
            job.Status = ValidationJobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
        }
        else
        {
            job.Status = ValidationJobStatus.Pending; // retry
        }

        job.FailureReason = reason;

        context.ValidationJobs.Update(job);

        return Task.CompletedTask;
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
    
    public Task CompleteJobAsync(
        ValidationJob job,
        ValidationResult result,
        CancellationToken cancellationToken)
    {
        context.ValidationResults.Add(result);

        job.Status = result.Status == ValidationStatus.Pass
            ? ValidationJobStatus.Completed
            : ValidationJobStatus.Failed;

        job.CompletedAt = DateTime.UtcNow;

        context.ValidationJobs.Update(job);

        return Task.CompletedTask;
    }
}
