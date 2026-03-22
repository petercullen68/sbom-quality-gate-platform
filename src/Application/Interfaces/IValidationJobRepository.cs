namespace SbomQualityGate.Application.Interfaces;

using Domain.Entities;

public interface IValidationJobRepository
{
    Task CreateAsync(ValidationJob job, CancellationToken cancellationToken);
    Task<ValidationJob?> ClaimNextPendingAsync(CancellationToken cancellationToken);
    Task UpdateAsync(ValidationJob job, CancellationToken cancellationToken);
    Task CompleteJobAsync(ValidationJob job, ValidationResult result, CancellationToken cancellationToken);
}