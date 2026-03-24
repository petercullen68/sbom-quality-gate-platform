using SbomQualityGate.Application.Models;
using SbomQualityGate.Application.UseCases;
using SbomQualityGate.Domain.Entities;
using SbomQualityGate.Domain.Enums;
using SbomQualityGate.UnitTests.Fakes;

namespace SbomQualityGate.UnitTests.UseCases;

public class ProcessNextValidationJobHandlerTests
{
    [Fact]
    public async Task HandleAsyncNoJobsReturnsFalse()
    {
        // Arrange
        var jobRepo = new FakeValidationJobRepository();
        var sbomRepo = new FakeSbomRepository();
        var validationTool = new FakeValidationTool();
        var unitOfWork = new FakeUnitOfWork();

        var handler = new ProcessNextValidationJobHandler(
            jobRepo,
            validationTool,
            sbomRepo,
            unitOfWork);

        // Act
        var result = await handler.HandleAsync(CancellationToken.None);

        // Assert
        Assert.False(result);
    }
    
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

        var sbomRepo = new FakeSbomRepository(); // returns null
        var validationTool = new FakeValidationTool();
        var unitOfWork = new FakeUnitOfWork();

        var handler = new ProcessNextValidationJobHandler(
            jobRepo,
            validationTool,
            sbomRepo,
            unitOfWork);

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

        var sbomRepo = new FakeSbomRepositoryWithData(); // returns valid SBOM

        var validationTool = new FakeValidationTool();

        var unitOfWork = new FakeUnitOfWork();

        var handler = new ProcessNextValidationJobHandler(
            jobRepo,
            validationTool,
            sbomRepo,
            unitOfWork);

        // Act
        var result = await handler.HandleAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.True(validationTool.WasCalled);
        Assert.True(jobRepo.CompleteCalled);
    }
    
    [Fact]
    public async Task HandleAsyncValidationToolThrowsReturnsFalse()
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

        var sbomRepo = new FakeSbomRepositoryWithData();

        var validationTool = new FakeValidationTool
        {
            ShouldThrow = true
        };

        var unitOfWork = new FakeUnitOfWork();

        var handler = new ProcessNextValidationJobHandler(
            jobRepo,
            validationTool,
            sbomRepo,
            unitOfWork);

        // Act
        var result = await handler.HandleAsync(CancellationToken.None);

        // Assert
        Assert.False(result);
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

        var sbomRepo = new FakeSbomRepositoryWithData();

        var validationTool = new FakeValidationTool
        {
            ResultToReturn = new ValidationToolResult
            {
                Status = ValidationStatus.Fail,
                Score = 40,
                ReportJson = "{}"
            }
        };

        var unitOfWork = new FakeUnitOfWork();

        var handler = new ProcessNextValidationJobHandler(
            jobRepo,
            validationTool,
            sbomRepo,
            unitOfWork);

        // Act
        var result = await handler.HandleAsync(CancellationToken.None);

        // Assert
        Assert.True(result); // important: processed successfully, even if failed
        Assert.True(jobRepo.CompleteCalled);
    }
}
