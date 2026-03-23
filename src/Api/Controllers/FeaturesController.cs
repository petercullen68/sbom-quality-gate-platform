using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SbomQualityGate.Application.UseCases;

namespace SbomQualityGate.Api.Controllers;

[ApiController]
[Route("api/features")]
public class FeaturesController(
    DiscoverSbomFeaturesHandler handler) : ControllerBase
{
    [HttpPost("discover")]
    public async Task<IActionResult> Discover(
        [FromBody] JsonElement report,
        CancellationToken cancellationToken)
    {
        try
        {
            await handler.HandleAsync(
                report.GetRawText(),
                cancellationToken);

            return Ok();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
