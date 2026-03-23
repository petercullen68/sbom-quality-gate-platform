using Microsoft.EntityFrameworkCore;
using SbomQualityGate.Application.Interfaces;

namespace SbomQualityGate.Infrastructure.Persistence;

public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public async Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken, bool notifyValidationJobs = false)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await action();

            await context.SaveChangesAsync(cancellationToken);

            if (notifyValidationJobs)
            {
                var payload = Guid.NewGuid().ToString();
#pragma warning disable EF1002
                await context.Database.ExecuteSqlRawAsync(
                    $"NOTIFY validation_jobs, '{payload}'", cancellationToken);
#pragma warning restore EF1002
                
            }
            
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
    
    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> action,
        CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await action();

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
