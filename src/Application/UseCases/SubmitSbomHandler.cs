namespace SbomQualityGate.Application.UseCases;

using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;

public class SubmitSbomHandler(ISbomRepository repository)
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

            // leave these empty for now (we’ll parse later)
            SpecType = string.Empty,
            SpecVersion = string.Empty,
            ComponentCount = 0,
            HasSupplier = false,
            HasLicenses = false
        };

        await repository.SaveAsync(sbom, cancellationToken);

        return sbom.Id;
    }
}