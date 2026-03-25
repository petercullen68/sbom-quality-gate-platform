using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.UnitTests.Fakes;

public class FailingCompleteValidationJobRepository : IValidationJobRepository
{
    public ValidationJob? JobToReturn { get; init; }
    public bool FailCalled { get; private set; }
    public ValidationJob? FailedJob { get; private set; }
    public string? FailReason { get; private set; }

    public Task<ValidationJob?> ClaimNextPendingAsync(CancellationToken cancellationToken)
        => Task.FromResult(JobToReturn);

    public Task AddAsync(ValidationJob job, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task CompleteJobAsync(ValidationJob job, ValidationResult result, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Simulated failure during completion");

    public Task FailJobAsync(ValidationJob job, string reason, CancellationToken cancellationToken)
    {
        FailCalled = true;
        FailedJob = job;
        FailReason = reason;
        return Task.CompletedTask;
    }
}
