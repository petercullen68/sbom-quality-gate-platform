using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;

namespace SbomQualityGate.Application.UseCases;

public class ProcessNextValidationJobHandler(
    IValidationJobRepository jobRepository,
    IValidationTool validationTool,
    ISbomRepository sbomRepository, 
    IUnitOfWork unitOfWork) 
{
    public async Task<bool> HandleAsync(CancellationToken cancellationToken)
    {
        return await unitOfWork.ExecuteAsync<bool>(async () =>
        {
            var job = await jobRepository.ClaimNextPendingAsync(cancellationToken);

            if (job == null)
            {
                return false;
            }
            
            try
            {
                var sbom = await sbomRepository.GetByIdAsync(job.SbomId, cancellationToken);

                if (sbom == null)
                {
                    await jobRepository.FailJobAsync(
                        job,
                        "SBOM not found",
                        cancellationToken);

                    return false;
                }

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

                await jobRepository.CompleteJobAsync(job, result, cancellationToken);
            }
            catch (Exception ex)
            {
                await jobRepository.FailJobAsync(
                    job,
                    ex.Message,
                    cancellationToken);
            }
            return true;
        }, cancellationToken);
    }
}
