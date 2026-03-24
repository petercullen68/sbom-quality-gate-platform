using Microsoft.EntityFrameworkCore;
using SbomQualityGate.Application.Interfaces;

namespace SbomQualityGate.Infrastructure.Persistence;

public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public async Task ExecuteAsync(
        Func<Task> action,
        CancellationToken cancellationToken,
        bool notifyValidationJobs = false)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await context.Database.BeginTransactionAsync(cancellationToken);

            await action();

            await context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            if (notifyValidationJobs)
            {
                await context.Database.ExecuteSqlRawAsync(
                    "NOTIFY validation_jobs",
                    cancellationToken);
            }
        });
    }
    
    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> action,
        CancellationToken cancellationToken)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await context.Database.BeginTransactionAsync(cancellationToken);

            var result = await action();

            await context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return result;
        });
    }
}
