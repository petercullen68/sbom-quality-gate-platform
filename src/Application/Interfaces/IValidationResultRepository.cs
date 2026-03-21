namespace SbomQualityGate.Application.Interfaces;

using SbomQualityGate.Domain.Entities;

public interface IValidationResultRepository
{
    Task SaveAsync(ValidationResult result, CancellationToken cancellationToken);
}