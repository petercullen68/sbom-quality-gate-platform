namespace SbomQualityGate.Application.UseCases;

using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;
using SbomQualityGate.Domain.Enums;

public class ProcessNextValidationJobHandler(
    IValidationJobRepository jobRepository,
    ISbomRepository sbomRepository,
    IValidationResultRepository resultRepository)
{
    public async Task<bool> HandleAsync(CancellationToken cancellationToken)
    {
        var job = await jobRepository.GetNextPendingAsync(cancellationToken);

        if (job is null)
        {
            return false;
        }

        job.Status = ValidationJobStatus.Pending;
        job.StartedAt = DateTime.UtcNow;
        await jobRepository.UpdateAsync(job, cancellationToken);

        var sbom = await sbomRepository.GetByIdAsync(job.SbomId, cancellationToken);

        if (sbom is null)
        {
            job.Status = ValidationJobStatus.Failed;
            job.CompletedAt = DateTime.UtcNow;
            await jobRepository.UpdateAsync(job, cancellationToken);
            return true;
        }

        await Task.Delay(1500, cancellationToken);

        var passed = !string.IsNullOrWhiteSpace(sbom.SbomJson);

        var result = new ValidationResult
        {
            Id = Guid.NewGuid(),
            SbomId = sbom.Id,
            Status = passed ? ValidationStatus.Pass : ValidationStatus.Fail,
            Score = passed ? 1.0 : 0.0,
            Profile = job.Profile,
            ReportJson = passed
                ? """{ "summary": "Validation passed" }"""
                : """{ "summary": "Validation failed" }""",
            CreatedAt = DateTime.UtcNow
        };

        await resultRepository.SaveAsync(result, cancellationToken);

        job.Status = result.Status == ValidationStatus.Pass ? ValidationJobStatus.Completed : ValidationJobStatus.Failed;
        job.CompletedAt = DateTime.UtcNow;
        await jobRepository.UpdateAsync(job, cancellationToken);

        return true;
    }
}