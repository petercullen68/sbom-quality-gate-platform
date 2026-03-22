using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.Application.Interfaces;

public interface IValidationResultRepository
{
    Task AddAsync(ValidationResult result, CancellationToken cancellationToken);
}