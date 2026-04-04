using Microsoft.EntityFrameworkCore;
using SbomQualityGate.Application.Interfaces;

namespace SbomQualityGate.Infrastructure.Persistence;

public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public Task ExecuteAsync(
        Func<Task> action,
        CancellationToken cancellationToken,
        bool notifyValidationJobs = false)
    {
        ArgumentNullException.ThrowIfNull(action);

        return ExecuteCoreAsync(
            async () =>
            {
                await action();
                return true;
            },
            notifyValidationJobs, cancellationToken);
    }

    public Task<T> ExecuteAsync<T>(
        Func<Task<T>> action,
        CancellationToken cancellationToken,
        bool notifyValidationJobs = false)
    {
        ArgumentNullException.ThrowIfNull(action);

        return ExecuteCoreAsync(action, notifyValidationJobs, cancellationToken);
    }

    private async Task<T> ExecuteCoreAsync<T>(
        Func<Task<T>> action,
        bool notifyValidationJobs,
        CancellationToken cancellationToken
        )
    {
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction =
                await context.Database.BeginTransactionAsync(cancellationToken);

            var result = await action();

            await context.SaveChangesAsync(cancellationToken);

            if (notifyValidationJobs)
            {
                await context.Database.ExecuteSqlRawAsync(
                    "NOTIFY validation_jobs",
                    cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);

            return result;
        });
    }
}
