using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SbomQualityGate.Application.Exceptions;
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
        catch (RequestValidationException ex)
        {
            ModelState.AddModelError("report", ex.Message);
            return ValidationProblem(ModelState);
        }
    }
}
