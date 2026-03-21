namespace SbomQualityGate.Application.Interfaces;

using SbomQualityGate.Domain.Entities;

public interface IValidationJobRepository
{
    Task CreateAsync(ValidationJob job, CancellationToken cancellationToken);
    Task<ValidationJob?> GetNextPendingAsync(CancellationToken cancellationToken);
    Task UpdateAsync(ValidationJob job, CancellationToken cancellationToken);
}