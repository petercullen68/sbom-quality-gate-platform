using Microsoft.AspNetCore.Mvc;
using SbomQualityGate.Application.UseCases;

namespace SbomQualityGate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SbomsController(SubmitSbomHandler handler) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitSbomCommand command, CancellationToken cancellationToken)
    {
        var id = await handler.HandleAsync(command, cancellationToken);

        return Ok(new { id });
    }
}