using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;
using SbomQualityGate.Domain.Enums;

namespace SbomQualityGate.Application.UseCases;

public class ProcessNextValidationJobHandler(
    IValidationJobRepository jobRepository)
{
    public async Task<bool> HandleAsync(CancellationToken cancellationToken)
    {
        var job = await jobRepository.ClaimNextPendingAsync(cancellationToken);

        if (job == null)
        {
            return false;
        }

        // process sbom → produce result
        var result = new ValidationResult
        {
            Id = Guid.NewGuid(),
            ValidationJobId = job.Id,
            Status = ValidationStatus.Pass, // or Fail
            ReportJson = "{}",
            CreatedAt = DateTime.UtcNow
        };

        await jobRepository.CompleteJobAsync(job, result, cancellationToken);

        return true;
    }
}