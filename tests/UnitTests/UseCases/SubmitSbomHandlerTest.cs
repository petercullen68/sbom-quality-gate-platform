using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Application.UseCases;
using SbomQualityGate.Domain.Enums;
using SbomQualityGate.UnitTests.Fakes;

namespace SbomQualityGate.UnitTests.UseCases;

public class SubmitSbomHandlerTest
{
    [Fact]
    public async Task HandleAsyncValidSbomPersistsSbomAndCreatesJob()
    {
        // Arrange
        var sbomRepo = new FakeSbomRepository();
        var jobRepo = new FakeValidationJobRepository();
        var unitOfWork = new FakeUnitOfWork();

        var handler = CreateHandler(sbomRepo: sbomRepo, jobRepo: jobRepo, unitOfWork: unitOfWork);

        var command = new SubmitSbomCommand
        {
            SbomJson = """
                       {
                           "bomFormat": "CycloneDX",
                           "specVersion": "1.5"
                       }
                       """
        };

        // Act
        await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.True(unitOfWork.Executed);
        Assert.True(unitOfWork.NotifyRequested);

        Assert.True(sbomRepo.AddCalled);
        Assert.NotNull(sbomRepo.AddedSbom);

        Assert.True(jobRepo.AddCalled);
        Assert.NotNull(jobRepo.AddedJob);

        Assert.Equal(sbomRepo.AddedSbom!.Id, jobRepo.AddedJob!.SbomId);
        Assert.Equal(ValidationJobStatus.Pending, jobRepo.AddedJob.Status);
        Assert.Equal("NIS2-Default", jobRepo.AddedJob.Profile);
    }

    [Fact]
    public async Task HandleAsyncInvalidJsonThrowsArgumentException()
    {
        // Arrange
        var sbomRepo = new FakeSbomRepository();
        var jobRepo = new FakeValidationJobRepository();
        var unitOfWork = new FakeUnitOfWork();

        var handler = CreateHandler(sbomRepo: sbomRepo, jobRepo: jobRepo, unitOfWork: unitOfWork);

        var command = new SubmitSbomCommand
        {
            SbomJson = "this-is-not-json"
        };

        // Act
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(command, CancellationToken.None));

        // Assert
        Assert.Equal("command", ex.ParamName);
        Assert.Contains("invalid JSON", ex.Message);

        Assert.False(sbomRepo.AddCalled);
        Assert.False(jobRepo.AddCalled);
    }

    [Fact]
    public async Task HandleAsyncJobCreationFailsThrowsAndDoesNotLeaveSystemInInvalidState()
    {
        // Arrange
        var sbomRepo = new FakeSbomRepository();
        var unitOfWork = new FakeUnitOfWork();
        var jobRepo = new FailingAddValidationJobRepository();

        var handler = CreateHandler(sbomRepo: sbomRepo, jobRepo: jobRepo, unitOfWork: unitOfWork);

        var command = new SubmitSbomCommand
        {
            SbomJson = """
                       {
                           "bomFormat": "CycloneDX",
                           "specVersion": "1.5"
                       }
                       """
        };

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(command, CancellationToken.None));

        Assert.True(unitOfWork.Executed);
        Assert.True(unitOfWork.NotifyRequested);
        Assert.True(sbomRepo.AddCalled);
    }

    [Fact]
    public async Task HandleAsyncExtractsSpecTypeAndVersion()
    {
        // Arrange
        var sbomRepo = new FakeSbomRepository();

        var handler = CreateHandler(sbomRepo: sbomRepo, jobRepo: null, unitOfWork: null);

        var command = new SubmitSbomCommand
        {
            SbomJson = """
                       {
                           "bomFormat": "CycloneDX",
                           "specVersion": "1.4"
                       }
                       """
        };

        // Act
        await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.Equal("CycloneDX", sbomRepo.AddedSbom!.SpecType);
        Assert.Equal("1.4", sbomRepo.AddedSbom.SpecVersion);
    }

    [Fact]
    public async Task HandleAsyncMissingMetadataThrowsArgumentExceptionAndDoesNotPersist()
    {
        // Arrange
        var sbomRepo = new FakeSbomRepository();
        var jobRepo = new FakeValidationJobRepository();

        var handler = CreateHandler(sbomRepo: sbomRepo, jobRepo: jobRepo, unitOfWork: null);

        var command = new SubmitSbomCommand
        {
            SbomJson = """
                       {
                           "someOtherField": "value"
                       }
                       """
        };

        // Act
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.HandleAsync(command, CancellationToken.None));

        // Assert
        Assert.Equal("command", ex.ParamName);
        Assert.Contains("valid CycloneDX or SPDX", ex.Message);

        Assert.False(sbomRepo.AddCalled);
        Assert.False(jobRepo.AddCalled);
    }

    private static SubmitSbomHandler CreateHandler(
        ISbomRepository? sbomRepo = null,
        IValidationJobRepository? jobRepo = null,
        IUnitOfWork? unitOfWork = null)
    {
        sbomRepo ??= new FakeSbomRepository();
        jobRepo ??= new FakeValidationJobRepository();
        unitOfWork ??= new FakeUnitOfWork();

        return new SubmitSbomHandler(
            sbomRepo,
            jobRepo,
            unitOfWork);
    }
}
