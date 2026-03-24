using SbomQualityGate.Application.Interfaces;

namespace SbomQualityGate.UnitTests.Fakes;

public class FakeUnitOfWork : IUnitOfWork
{
    public Task ExecuteAsync(
        Func<Task> action,
        CancellationToken cancellationToken,
        bool notifyValidationJobs = false)
    {
        return action();
    }

    public Task<T> ExecuteAsync<T>(
        Func<Task<T>> action,
        CancellationToken cancellationToken)
    {
        return action();
    }
}
