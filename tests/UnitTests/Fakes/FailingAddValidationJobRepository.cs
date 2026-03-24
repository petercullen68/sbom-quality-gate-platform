using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.UnitTests.Fakes;

public sealed class FailingAddValidationJobRepository : IValidationJobRepository
{
    public Task<ValidationJob?> ClaimNextPendingAsync(CancellationToken cancellationToken)
        => Task.FromResult<ValidationJob?>(null);

    public Task AddAsync(ValidationJob job, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Simulated DB failure");

    public Task CompleteJobAsync(ValidationJob job, ValidationResult result, CancellationToken cancellationToken)
        => Task.CompletedTask;

    public Task FailJobAsync(ValidationJob job, string reason, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
