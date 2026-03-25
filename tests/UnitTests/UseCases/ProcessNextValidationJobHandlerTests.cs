using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Application.Models;
using SbomQualityGate.Application.UseCases;
using SbomQualityGate.Domain.Entities;
using SbomQualityGate.Domain.Enums;
using SbomQualityGate.UnitTests.Fakes;

namespace SbomQualityGate.UnitTests.UseCases;

public class ProcessNextValidationJobHandlerTests
{
    [Fact]
    public async Task HandleAsyncJobExistsButSbomMissingReturnsFalse()
    {
        // Arrange
        var jobRepo = new FakeValidationJobRepository
        {
            JobToReturn = new ValidationJob
            {
                Id = Guid.NewGuid(),
                SbomId = Guid.NewGuid(),
                Status = ValidationJobStatus.Pending
            }
        };

        var handler = CreateHandler(jobRepo: jobRepo);

        // Act
        var result = await handler.HandleAsync(CancellationToken.None);

        // Assert
        Assert.False(result);
        Assert.True(jobRepo.FailCalled);
        Assert.Equal("SBOM not found", jobRepo.FailReason);
        Assert.NotNull(jobRepo.FailedJob);
        Assert.False(jobRepo.CompleteCalled);
    }

    [Fact]
    public async Task HandleAsyncNoJobsReturnsFalse()
    {
        // Arrange
        var jobRepo = new FakeValidationJobRepository
        {
            JobToReturn = null
        };

        var handler = CreateHandler(jobRepo: jobRepo);

        // Act
        var result = await handler.HandleAsync(CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HandleAsyncValidJobProcessesSuccessfully()
    {
        // Arrange
        var jobRepo = new FakeValidationJobRepository
        {
            JobToReturn = new ValidationJob
            {
                Id = Guid.NewGuid(),
                SbomId = Guid.NewGuid(),
                Status = ValidationJobStatus.Pending
            }
        };

        var validationTool = new FakeValidationTool();
        var fakeSbomRepoWithData = new FakeSbomRepositoryWithData();

        var handler = CreateHandler(
            jobRepo: jobRepo,
            sbomRepo: fakeSbomRepoWithData,
            validationTool: validationTool);

        // Act
        var result = await handler.HandleAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.True(validationTool.WasCalled);
        Assert.True(jobRepo.CompleteCalled);
    }

    [Fact]
    public async Task HandleAsyncValidationToolThrowsFailsJobAndReturnsFalse()
    {
        // Arrange
        var jobRepo = new FakeValidationJobRepository
        {
            JobToReturn = new ValidationJob
            {
                Id = Guid.NewGuid(),
                SbomId = Guid.NewGuid(),
                Status = ValidationJobStatus.Pending
            }
        };

        var validationTool = new FakeValidationTool
        {
            ShouldThrow = true
        };

        var sbomRepo = new FakeSbomRepositoryWithData();
        var handler = CreateHandler(jobRepo: jobRepo, sbomRepo: sbomRepo, validationTool: validationTool);

        // Act
        var result = await handler.HandleAsync(CancellationToken.None);

        // Assert
        Assert.False(result);
        Assert.True(validationTool.WasCalled);
        Assert.True(jobRepo.FailCalled);
        Assert.Contains("Simulated validation tool failure", jobRepo.FailReason);
    }

    [Fact]
    public async Task HandleAsyncCompleteJobFailsAndDoesNotReportSuccess()
    {
        // Arrange
        var sbom = new Sbom
        {
            Id = Guid.NewGuid(),
            SbomJson = "{}"
        };

        var job = new ValidationJob
        {
            Id = Guid.NewGuid(),
            SbomId = sbom.Id,
            Status = ValidationJobStatus.Pending
        };

        var sbomRepo = new FakeSbomRepository
        {
            GetByIdFunc = _ => sbom
        };

        var jobRepo = new FailingCompleteValidationJobRepository
        {
            JobToReturn = job
        };

        var validationTool = new FakeValidationTool
        {
            ResultToReturn = new ValidationToolResult
            {
                Status = ValidationStatus.Pass,
                Score = 90,
                ReportJson = "{}"
            }
        };

        var unitOfWork = new FakeUnitOfWork();

        var handler = CreateHandler(
            jobRepo: jobRepo,
            sbomRepo: sbomRepo,
            validationTool: validationTool,
            unitOfWork: unitOfWork);

        // Act
        var result = await handler.HandleAsync(CancellationToken.None);

        // Assert
        Assert.False(result);
        Assert.True(unitOfWork.Executed);
        Assert.True(jobRepo.FailCalled);
        Assert.Equal(job.Id, jobRepo.FailedJob?.Id);
        Assert.Contains("Simulated failure during completion", jobRepo.FailReason);
    }

    [Fact]
    public async Task HandleAsyncValidationFailsReturnsTrue()
    {
        // Arrange
        var jobRepo = new FakeValidationJobRepository
        {
            JobToReturn = new ValidationJob
            {
                Id = Guid.NewGuid(),
                SbomId = Guid.NewGuid(),
                Status = ValidationJobStatus.Pending
            }
        };

        var validationTool = new FakeValidationTool
        {
            ResultToReturn = new ValidationToolResult
            {
                Status = ValidationStatus.Fail,
                Score = 40,
                ReportJson = "{}"
            }
        };

        var fakeSbomRepoWithData = new FakeSbomRepositoryWithData();

        var handler = CreateHandler(jobRepo: jobRepo, sbomRepo: fakeSbomRepoWithData, validationTool: validationTool);

        // Act
        var result = await handler.HandleAsync(CancellationToken.None);

        // Assert
        Assert.True(result); // processed successfully even when validation status is Fail
        Assert.True(jobRepo.CompleteCalled);
    }

    private static ProcessNextValidationJobHandler CreateHandler(
        IValidationJobRepository? jobRepo = null,
        ISbomRepository? sbomRepo = null,
        IValidationTool? validationTool = null,
        IUnitOfWork? unitOfWork = null)
    {
        jobRepo ??= new FakeValidationJobRepository();
        sbomRepo ??= new FakeSbomRepository();
        validationTool ??= new FakeValidationTool();
        unitOfWork ??= new FakeUnitOfWork();

        return new ProcessNextValidationJobHandler(
            jobRepo,
            validationTool,
            sbomRepo,
            unitOfWork);
    }
}
