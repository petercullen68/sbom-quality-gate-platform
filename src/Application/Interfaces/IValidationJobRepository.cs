namespace SbomQualityGate.Application.Interfaces;

using Domain.Entities;

public interface IValidationJobRepository
{
    Task AddAsync(ValidationJob job, CancellationToken cancellationToken);
    Task<ValidationJob?> ClaimNextPendingAsync(CancellationToken cancellationToken);
    Task CompleteJobAsync(ValidationJob job, ValidationResult result, CancellationToken cancellationToken);
    Task FailJobAsync(ValidationJob job, string reason, CancellationToken cancellationToken);
}
