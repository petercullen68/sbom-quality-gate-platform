namespace SbomQualityGate.Application.Interfaces;

public interface IUnitOfWork
{
    Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken, bool notifyValidationJobs = false);
    Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken);
}
