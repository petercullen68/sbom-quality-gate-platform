using SbomQualityGate.Application.Interfaces;

namespace SbomQualityGate.UnitTests.Fakes;

/// <summary>
/// Lets the first ExecuteAsync call through (phase 1 — claim),
/// then throws OperationCanceledException on the second call (phase 3 — persist).
/// Used to verify that worker shutdown during the persist phase does not mutate job state.
/// </summary>
public class CancellingOnPersistUnitOfWork : IUnitOfWork
{
    private int _callCount;

    public async Task ExecuteAsync(
        Func<Task> action,
        CancellationToken cancellationToken,
        bool notifyValidationJobs = false)
    {
        _callCount++;

        if (_callCount >= 2)
            throw new OperationCanceledException(cancellationToken);

        await action();
    }

    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> action, 
        CancellationToken cancellationToken, 
        bool notifyValidationJobs = false)
    {
        _callCount++;

        if (_callCount >= 2)
            throw new OperationCanceledException(cancellationToken);

        return await action();
    }
}
