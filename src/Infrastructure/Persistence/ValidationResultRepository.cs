using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.Infrastructure.Persistence;

public class ValidationResultRepository(AppDbContext context) : IValidationResultRepository
{
    public Task AddAsync(ValidationResult result, CancellationToken cancellationToken)
    {
        context.ValidationResults.Add(result);
        return Task.CompletedTask;
    }
}