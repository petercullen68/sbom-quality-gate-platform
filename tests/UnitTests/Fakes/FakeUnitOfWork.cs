using SbomQualityGate.Application.Interfaces;

namespace SbomQualityGate.UnitTests.Fakes;

public class FakeUnitOfWork : IUnitOfWork
{
    public bool Executed { get; private set; }
    public bool NotifyRequested { get; private set; }

    public async Task ExecuteAsync(
        Func<Task> action,
        CancellationToken cancellationToken,
        bool notifyValidationJobs = false)
    {
        Executed = true;
        NotifyRequested = notifyValidationJobs;

        await action();
    }

    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> action,
        CancellationToken cancellationToken)
    {
        Executed = true;
        return await action();
    }
}
