using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;
using SbomQualityGate.Domain.Enums;

namespace SbomQualityGate.UnitTests.Fakes;

public class FakeValidationJobRepository : IValidationJobRepository
{
    public ValidationJob? JobToReturn { get; init; }

    public bool AddCalled { get; private set; }
    public ValidationJob? AddedJob { get; private set; }

    public bool CompleteCalled { get; private set; }
    public ValidationJob? CompletedJob { get; private set; }
    public ValidationResult? SavedResult { get; private set; }
    public ValidationJobStatus? CompletedJobStatus { get; private set; }  // ← new

    public bool FailCalled { get; private set; }
    public ValidationJob? FailedJob { get; private set; }
    public string? FailReason { get; private set; }

    public Task<ValidationJob?> ClaimNextPendingAsync(CancellationToken cancellationToken)
        => Task.FromResult(JobToReturn);

    public Task AddAsync(ValidationJob job, CancellationToken cancellationToken)
    {
        AddCalled = true;
        AddedJob = job;
        return Task.CompletedTask;
    }

    public Task CompleteJobAsync(ValidationJob job, ValidationResult result, CancellationToken cancellationToken)
    {
        job.Status = ValidationJobStatus.Completed;  // ← mirror real repository behaviour
        CompleteCalled = true;
        CompletedJob = job;
        SavedResult = result;
        CompletedJobStatus = job.Status;
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
