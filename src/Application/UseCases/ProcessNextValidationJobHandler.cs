using Microsoft.Extensions.Logging;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;
using SbomQualityGate.Domain.Enums;

namespace SbomQualityGate.Application.UseCases;

public class ProcessNextValidationJobHandler(
    IValidationJobRepository jobRepository,
    IValidationTool validationTool,
    ISpecConformanceTool specConformanceTool,
    ISbomRepository sbomRepository,
    DiscoverSbomReportHandler discoverReportHandler,
    IUnitOfWork unitOfWork,
    ILogger<ProcessNextValidationJobHandler> logger)
{
    private static readonly Action<ILogger, Guid, Exception?> JobClaimed =
        LoggerMessage.Define<Guid>(
            LogLevel.Information,
            new EventId(21, nameof(ProcessNextValidationJobHandler)),
            "Job {JobId} claimed — beginning validation pipeline");

    private static readonly Action<ILogger, Guid, Exception?> JobCompleted =
        LoggerMessage.Define<Guid>(
            LogLevel.Information,
            new EventId(22, nameof(ProcessNextValidationJobHandler)),
            "Job {JobId} completed successfully");

    private static readonly Action<ILogger, Guid, Exception?> JobFailed =
        LoggerMessage.Define<Guid>(
            LogLevel.Warning,
            new EventId(23, nameof(ProcessNextValidationJobHandler)),
            "Job {JobId} failed");

    private static readonly Action<ILogger, Exception?> DiscoveryFailed =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(24, nameof(ProcessNextValidationJobHandler)),
            "Discovery failed after job completion — catalogue may be stale");

    public async Task<bool> HandleAsync(CancellationToken cancellationToken)
    {
        // -------------------------------------------------------
        // Phase 1: claim the next pending job — short transaction
        // -------------------------------------------------------
        var job = await unitOfWork.ExecuteAsync(async () =>
            await jobRepository.ClaimNextPendingAsync(cancellationToken),
            cancellationToken);

        if (job is null)
            return false;

        JobClaimed(logger, job.Id, null);

        // -------------------------------------------------------
        // Phase 2: external work — outside any transaction
        // -------------------------------------------------------
        string reportJson;
        SpecConformanceStatus isSpecConformant;
        string[] deprecationWarnings;
        ValidationStatus status;
        double score;

        try
        {
            var sbom = await sbomRepository.GetByIdAsync(job.SbomId, cancellationToken);

            if (sbom is null)
            {
                await FailJobAsync(job, "SBOM not found", cancellationToken);
                return false;
            }

            var resultData = await validationTool.ValidateAsync(
                sbom.SbomJson,
                job.Profile,
                cancellationToken);

            var conformanceResult = await specConformanceTool.CheckAsync(
                sbom.SbomJson,
                sbom.SpecType,
                sbom.SpecVersion,
                cancellationToken);

            reportJson = resultData.ReportJson;
            status = resultData.Status;
            score = resultData.Score;
            isSpecConformant = conformanceResult.Status;
            deprecationWarnings = [..conformanceResult.DeprecationWarnings];
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await FailJobAsync(job, ex.Message, cancellationToken);
            return false;
        }

        // -------------------------------------------------------
        // Phase 3: persist result — short transaction
        // -------------------------------------------------------
        try
        {
            await unitOfWork.ExecuteAsync(async () =>
            {
                var result = new ValidationResult
                {
                    Id = Guid.NewGuid(),
                    ValidationJobId = job.Id,
                    Status = status,
                    Score = score,
                    ReportJson = reportJson,
                    Profile = job.Profile,
                    SpecConformanceStatus = isSpecConformant,
                    DeprecationWarnings = deprecationWarnings,
                    CreatedAt = DateTime.UtcNow
                };

                await jobRepository.CompleteJobAsync(job, result, cancellationToken);
            }, cancellationToken);

            JobCompleted(logger, job.Id, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            await FailJobAsync(job, ex.Message, cancellationToken);
            return false;
        }

        // -------------------------------------------------------
        // Phase 4: best-effort discovery — outside transaction
        // -------------------------------------------------------
        if (!string.IsNullOrEmpty(reportJson))
        {
            try
            {
                await discoverReportHandler.HandleAsync(reportJson, cancellationToken);
            }
            catch (Exception ex)
            {
                DiscoveryFailed(logger, ex);
            }
        }

        return true;
    }

    private async Task FailJobAsync(
        ValidationJob job,
        string reason,
        CancellationToken cancellationToken)
    {
        JobFailed(logger, job.Id, null);

        await unitOfWork.ExecuteAsync(async () =>
            await jobRepository.FailJobAsync(job, reason, cancellationToken),
            cancellationToken);
    }
}
