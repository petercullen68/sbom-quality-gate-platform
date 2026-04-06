using SbomQualityGate.Application.Exceptions;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Application.UseCases;
using SbomQualityGate.Domain.Entities;
using SbomQualityGate.Domain.Enums;
using SbomQualityGate.UnitTests.Fakes;

namespace SbomQualityGate.UnitTests.UseCases;

public class SubmitSbomHandlerTest
{
    private static readonly Guid ValidProductId = Guid.NewGuid();
    
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
            ProductId = ValidProductId,
            Version = "1.0.0",
            SbomContent = """
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
        Assert.Equal(ValidProductId, sbomRepo.AddedSbom!.ProductId);

        Assert.True(jobRepo.AddCalled);
        Assert.NotNull(jobRepo.AddedJob);

        Assert.Equal(sbomRepo.AddedSbom.Id, jobRepo.AddedJob!.SbomId);
        Assert.Equal(ValidationJobStatus.Pending, jobRepo.AddedJob.Status);
        Assert.Equal("NIS2-Default", jobRepo.AddedJob.Profile);
    }

    [Fact]
    public async Task HandleAsyncProductNotFoundThrowsRequestValidationException()
    {
        // Arrange
        var handler = CreateHandler(productRepo: new FakeProductRepository(productToReturn: null));

        var command = new SubmitSbomCommand
        {
            ProductId = Guid.NewGuid(),
            Version = "1.0.0",
            SbomContent = """
                          {
                              "bomFormat": "CycloneDX",
                              "specVersion": "1.5"
                          }
                          """
        };

        // Act
        var ex = await Assert.ThrowsAsync<RequestValidationException>(() =>
            handler.HandleAsync(command, CancellationToken.None));

        // Assert
        Assert.Contains("does not exist", ex.Message);
    }

    [Fact]
    public async Task HandleAsyncProductNotFoundDoesNotPersistAnything()
    {
        // Arrange
        var sbomRepo = new FakeSbomRepository();
        var jobRepo = new FakeValidationJobRepository();
        var handler = CreateHandler(
            sbomRepo: sbomRepo,
            jobRepo: jobRepo,
            productRepo: new FakeProductRepository(productToReturn: null));

        var command = new SubmitSbomCommand
        {
            ProductId = Guid.NewGuid(),
            Version = "1.0.0",
            SbomContent = """
                          {
                              "bomFormat": "CycloneDX",
                              "specVersion": "1.5"
                          }
                          """
        };

        // Act
        await Assert.ThrowsAsync<RequestValidationException>(() =>
            handler.HandleAsync(command, CancellationToken.None));

        // Assert
        Assert.False(sbomRepo.AddCalled);
        Assert.False(jobRepo.AddCalled);
    }

    [Fact]
    public async Task HandleAsyncNoSystemProfilesExistThrowsRequestValidationException()
    {
        // Arrange
        var profileRepo = new FakeSbomProfileRepository(anySystemProfilesExist: false);
        var handler = CreateHandler(profileRepo: profileRepo);

        var command = new SubmitSbomCommand
        {
            ProductId = ValidProductId,
            Version = "1.0.0",
            SbomContent = """
                          {
                              "bomFormat": "CycloneDX",
                              "specVersion": "1.5"
                          }
                          """
        };

        // Act
        var ex = await Assert.ThrowsAsync<RequestValidationException>(() =>
            handler.HandleAsync(command, CancellationToken.None));

        // Assert
        Assert.Contains("No SBOM quality profiles have been discovered", ex.Message);
    }

    [Fact]
    public async Task HandleAsyncNoSystemProfilesExistDoesNotPersistAnything()
    {
        // Arrange
        var sbomRepo = new FakeSbomRepository();
        var jobRepo = new FakeValidationJobRepository();
        var profileRepo = new FakeSbomProfileRepository(anySystemProfilesExist: false);

        var handler = CreateHandler(sbomRepo: sbomRepo, jobRepo: jobRepo, profileRepo: profileRepo);

        var command = new SubmitSbomCommand
        {
            ProductId = ValidProductId,
            Version = "1.0.0",
            SbomContent = """
                          {
                              "bomFormat": "CycloneDX",
                              "specVersion": "1.5"
                          }
                          """
        };

        // Act
        await Assert.ThrowsAsync<RequestValidationException>(() =>
            handler.HandleAsync(command, CancellationToken.None));

        // Assert
        Assert.False(sbomRepo.AddCalled);
        Assert.False(jobRepo.AddCalled);
    }

    [Fact]
    public async Task HandleAsyncInvalidJsonThrowsRequestValidationException()
    {
        // Arrange
        var handler = CreateHandler();

        var command = new SubmitSbomCommand
        {
            ProductId = ValidProductId,
            Version = "1.0.0",
            SbomContent = "this-is-not-json"
        };

        // Act
        var ex = await Assert.ThrowsAsync<RequestValidationException>(() =>
            handler.HandleAsync(command, CancellationToken.None));

        // Assert
        Assert.Contains("invalid JSON", ex.Message);
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
            ProductId = ValidProductId,
            Version = "1.0.0",
            SbomContent = """
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
        var handler = CreateHandler(sbomRepo: sbomRepo);

        var command = new SubmitSbomCommand
        {
            ProductId = ValidProductId,
            Version = "1.0.0",
            SbomContent = """
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
    public async Task HandleAsyncMissingMetadataThrowsRequestValidationExceptionAndDoesNotPersist()
    {
        // Arrange
        var sbomRepo = new FakeSbomRepository();
        var jobRepo = new FakeValidationJobRepository();
        var handler = CreateHandler(sbomRepo: sbomRepo, jobRepo: jobRepo);

        var command = new SubmitSbomCommand
        {
            ProductId = ValidProductId,
            Version = "1.0.0",
            SbomContent = """
                          {
                              "someOtherField": "value"
                          }
                          """
        };

        // Act
        var ex = await Assert.ThrowsAsync<RequestValidationException>(() =>
            handler.HandleAsync(command, CancellationToken.None));

        // Assert
        Assert.Contains("valid CycloneDX or SPDX", ex.Message);
        Assert.False(sbomRepo.AddCalled);
        Assert.False(jobRepo.AddCalled);
    }

    private static SubmitSbomHandler CreateHandler(
        ISbomRepository? sbomRepo = null,
        IValidationJobRepository? jobRepo = null,
        IProductRepository? productRepo = null,
        ISbomProfileRepository? profileRepo = null,
        ISbomFormatDetector? formatDetector = null,
        ISbomXmlConverter? xmlConverter = null,
        IUnitOfWork? unitOfWork = null)
    {
        sbomRepo ??= new FakeSbomRepository();
        jobRepo ??= new FakeValidationJobRepository();
        productRepo ??= new FakeProductRepository(new Product
        {
            Id = Guid.NewGuid(),
            TeamId = Guid.NewGuid(),
            Name = "Default Product",
            CreatedAt = DateTime.UtcNow
        });
        profileRepo ??= new FakeSbomProfileRepository();
        formatDetector ??= new FakeSbomFormatDetector();
        xmlConverter ??= new FakeSbomXmlConverter();
        unitOfWork ??= new FakeUnitOfWork();

        return new SubmitSbomHandler(
            sbomRepo,
            jobRepo,
            productRepo,
            profileRepo,
            formatDetector,
            xmlConverter,
            unitOfWork);
    }
}
