using System.Text.Json;
using SbomQualityGate.Application.Exceptions;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Domain.Entities;
using SbomQualityGate.Domain.Enums;

namespace SbomQualityGate.Application.UseCases;

public class SubmitSbomHandler(
    ISbomRepository repository,
    IValidationJobRepository jobRepository,
    IProductRepository productRepository,
    ISbomProfileRepository profileRepository,
    IUnitOfWork unitOfWork) : ISubmitSbomHandler
{
    public async Task<Guid> HandleAsync(SubmitSbomCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Guard: profiles must be discovered before SBOMs can be submitted.
        if (!await profileRepository.AnySystemProfilesExistAsync(cancellationToken))
        {
            throw new RequestValidationException(
                "No SBOM quality profiles have been discovered. " +
                "Please submit a sbomqs report to the discovery endpoint before uploading SBOMs.");
        }

        // Guard: product must exist.
        var product = await productRepository.GetByIdAsync(command.ProductId, cancellationToken);
        if (product is null)
        {
            throw new RequestValidationException(
                $"Product '{command.ProductId}' does not exist.");
        }

        var specType = string.Empty;
        var specVersion = string.Empty;
        var componentCount = 0;

        try
        {
            using var doc = JsonDocument.Parse(command.SbomJson);
            var root = doc.RootElement;

            // Detect CycloneDX
            if (root.TryGetProperty("bomFormat", out var bomFormatProp))
            {
                specType = bomFormatProp.GetString();

                if (root.TryGetProperty("specVersion", out var specVersionProp))
                {
                    specVersion = specVersionProp.GetString() ?? string.Empty;
                }

                if (root.TryGetProperty("components", out var componentsProp) &&
                    componentsProp.ValueKind == JsonValueKind.Array)
                {
                    componentCount = componentsProp.GetArrayLength();
                }
            }
            // Detect SPDX
            else if (root.TryGetProperty("spdxVersion", out var spdxVersionProp))
            {
                specType = "SPDX";
                specVersion = spdxVersionProp.GetString() ?? string.Empty;

                if (root.TryGetProperty("packages", out var packagesProp) &&
                    packagesProp.ValueKind == JsonValueKind.Array)
                {
                    componentCount = packagesProp.GetArrayLength();
                }
            }
        }
        catch (JsonException)
        {
            throw new RequestValidationException("command.SbomJson contains invalid JSON.");
        }
        catch (InvalidOperationException)
        {
            throw new RequestValidationException("command.SbomJson has an invalid structure.");
        }

        if (string.IsNullOrEmpty(specType) || (specType != "CycloneDX" && specType != "SPDX"))
        {
            throw new RequestValidationException(
                "command.SbomJson must be a valid CycloneDX or SPDX document.");
        }

        var sbom = new Sbom
        {
            Id = Guid.NewGuid(),
            ProductId = command.ProductId,
            Version = command.Version,
            SbomJson = command.SbomJson,
            UploadedAt = DateTime.UtcNow,
            SpecType = specType,
            SpecVersion = specVersion,
            ComponentCount = componentCount,
        };

        await unitOfWork.ExecuteAsync(async () =>
        {
            await repository.AddAsync(sbom, cancellationToken);

            var job = new ValidationJob
            {
                Id = Guid.NewGuid(),
                SbomId = sbom.Id,
                Status = ValidationJobStatus.Pending,
                Profile = "NIS2-Default",
                CreatedAt = DateTime.UtcNow
            };

            await jobRepository.AddAsync(job, cancellationToken);
        }, cancellationToken, true);

        return sbom.Id;
    }
}
