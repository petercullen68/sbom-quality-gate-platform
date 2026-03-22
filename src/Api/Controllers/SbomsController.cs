using Microsoft.AspNetCore.Mvc;
using SbomQualityGate.Api.Models;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Application.UseCases;

namespace SbomQualityGate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SbomsController(ISubmitSbomHandler handler, ISbomRepository sbomRepository) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitSbomCommand command, CancellationToken cancellationToken)
    {
        var id = await handler.HandleAsync(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            new { id });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SbomResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SbomResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var sbom = await sbomRepository.GetByIdAsync(id, cancellationToken);

        if (sbom == null)
        {
            return NotFound();
        }

        var response = new SbomResponse
        {
            Id = sbom.Id,
            Team = sbom.Team,
            Project = sbom.Project,
            Version = sbom.Version,
            SpecType = sbom.SpecType,
            SpecVersion = sbom.SpecVersion,
            ComponentCount = sbom.ComponentCount,
            UploadedAt = sbom.UploadedAt
        };

        return Ok(response);
    }
}
