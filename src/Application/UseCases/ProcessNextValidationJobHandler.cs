using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;
using SbomQualityGate.Domain.Enums;

namespace SbomQualityGate.Application.UseCases;

public class ProcessNextValidationJobHandler(
    IValidationJobRepository jobRepository,
    IValidationTool validationTool,
    ISbomRepository sbomRepository, 
    IUnitOfWork unitOfWork) 
{
    public async Task<bool> HandleAsync(CancellationToken cancellationToken)
    {
        var job = await jobRepository.ClaimNextPendingAsync(cancellationToken);

        if (job == null)
        {
            return false;
        }

        var sbom = await sbomRepository.GetByIdAsync(job.SbomId, cancellationToken);
        if (sbom != null)
        {
            var resultData = await validationTool.ValidateAsync(
                sbom.SbomJson,
                job.Profile,
                cancellationToken);

            var result = new ValidationResult
            {
                Id = Guid.NewGuid(),
                ValidationJobId = job.Id,
                Status = resultData.Status,
                Score = resultData.Score,
                ReportJson = resultData.ReportJson,
                Profile = job.Profile,
                CreatedAt = DateTime.UtcNow
            };

            await unitOfWork.ExecuteAsync(async () =>
            {
                await jobRepository.CompleteJobAsync(job, result, cancellationToken);

            }, cancellationToken);

            return true;
        }

        throw new InvalidOperationException($"SBOM not found for job {job.Id}");
    }
}
