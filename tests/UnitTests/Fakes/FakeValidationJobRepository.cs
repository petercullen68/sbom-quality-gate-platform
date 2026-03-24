using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.UnitTests.Fakes;

public class FakeValidationJobRepository : IValidationJobRepository
{
    public ValidationJob? JobToReturn { get; init; }

    public bool CompleteCalled { get; private set; }

    public Task<ValidationJob?> ClaimNextPendingAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(JobToReturn);
    }

    public Task CompleteJobAsync(ValidationJob job, ValidationResult result, CancellationToken cancellationToken)
    {
        CompleteCalled = true;
        return Task.CompletedTask;
    }
    public Task AddAsync(ValidationJob job, CancellationToken cancellationToken)
    {
        return Task.FromResult<ValidationJob?>(null);
    }
    
    public Task FailJobAsync(ValidationJob job, string reason, CancellationToken cancellationToken)
    {
        return Task.FromResult<ValidationJob?>(null);
    }
}
