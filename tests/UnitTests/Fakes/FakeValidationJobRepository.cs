using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.UnitTests.Fakes;

public class FakeValidationJobRepository : IValidationJobRepository
{
    public ValidationJob? JobToReturn { get; init; }

    public bool AddCalled { get; private set; }
    public ValidationJob? AddedJob { get; private set; }

    public bool CompleteCalled { get; private set; }
    public ValidationJob? CompletedJob { get; private set; }
    public ValidationResult? SavedResult { get; private set; }

    public bool FailCalled { get; private set; }
    public ValidationJob? FailedJob { get; private set; }
    public string? FailReason { get; private set; }

    public Task<ValidationJob?> ClaimNextPendingAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(JobToReturn);
    }

    public Task AddAsync(ValidationJob job, CancellationToken cancellationToken)
    {
        AddCalled = true;
        AddedJob = job;
        return Task.CompletedTask;
    }

    public Task CompleteJobAsync(ValidationJob job, ValidationResult result, CancellationToken cancellationToken)
    {
        CompleteCalled = true;
        CompletedJob = job;
        SavedResult = result;
        return Task.CompletedTask;
    }

    public Task FailJobAsync(ValidationJob job, string reason, CancellationToken cancellationToken)
    {
        FailCalled = true;
        FailedJob = job;
        FailReason = reason;
        return Task.CompletedTask;
    }
}
