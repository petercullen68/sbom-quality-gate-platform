using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;
using SbomQualityGate.Domain.Enums;

namespace SbomQualityGate.Application.UseCases;
public class SubmitSbomHandler(ISbomRepository repository, IValidationJobRepository jobRepository)
{
    public async Task<Guid> HandleAsync(SubmitSbomCommand command, CancellationToken cancellationToken)
    {
        var sbom = new Sbom
        {
            Id = Guid.NewGuid(),
            Team = command.Team,
            Project = command.Project,
            Version = command.Version,
            SbomJson = command.SbomJson,
            UploadedAt = DateTime.UtcNow,
            SpecType = string.Empty,
            SpecVersion = string.Empty,
            ComponentCount = 0,
            HasSupplier = false,
            HasLicenses = false
        };

        await repository.SaveAsync(sbom, cancellationToken);

        var job = new ValidationJob
        {
            Id = Guid.NewGuid(),
            SbomId = sbom.Id,
            Status = ValidationJobStatus.Pending,
            Profile = "NIS2-Default",
            CreatedAt = DateTime.UtcNow
        };

        await jobRepository.CreateAsync(job, cancellationToken);

        return sbom.Id;
    }
}