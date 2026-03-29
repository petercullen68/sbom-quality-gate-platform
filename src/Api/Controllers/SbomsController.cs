using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SbomQualityGate.Api.Configuration;
using SbomQualityGate.Api.Models;
using SbomQualityGate.Application.Exceptions;
using SbomQualityGate.Application.Interfaces;
using SbomQualityGate.Application.UseCases;

namespace SbomQualityGate.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SbomsController(
    ISubmitSbomHandler handler,
    ISbomRepository sbomRepository,
    IOptions<UploadOptions> uploadOptions) : ControllerBase
{
    private sealed class UploadTooLargeException(string message) : Exception(message);

    [HttpPost]
    public Task<IActionResult> Submit([FromBody] SubmitSbomCommand command, CancellationToken cancellationToken) =>
        SubmitInternalAsync(command, cancellationToken);

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
        [FromForm] UploadSbomRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            ModelState.AddModelError(nameof(request.File), "File is required.");
            return ValidationProblem(ModelState);
        }

        var maxUploadBytes = uploadOptions.Value.MaxUploadBytes;
        if (maxUploadBytes <= 0)
        {
            return Problem(
                title: "Upload limits are not configured",
                detail: "Upload:MaxUploadBytes must be greater than zero.",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        if (request.File.Length > maxUploadBytes)
        {
            return Problem(
                title: "File too large",
                detail: $"Maximum allowed size is {maxUploadBytes} bytes.",
                statusCode: StatusCodes.Status413PayloadTooLarge);
        }

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        string sbomJson;
        try
        {
            sbomJson = await ReadUtf8WithLimitAsync(request.File, maxUploadBytes, cancellationToken);
        }
        catch (UploadTooLargeException)
        {
            return Problem(
                title: "File too large",
                detail: $"Maximum allowed size is {maxUploadBytes} bytes.",
                statusCode: StatusCodes.Status413PayloadTooLarge);
        }

        if (!IsValidJson(sbomJson))
        {
            ModelState.AddModelError(nameof(request.File), "Uploaded file is not valid JSON.");
            return ValidationProblem(ModelState);
        }

        var command = new SubmitSbomCommand
        {
            Team = request.Team.Trim(),
            Project = request.Project.Trim(),
            Version = request.Version.Trim(),
            SbomJson = sbomJson
        };

        return await SubmitInternalAsync(command, cancellationToken);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SbomResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SbomResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var sbom = await sbomRepository.GetByIdAsync(id, cancellationToken);

        if (sbom is null)
            return NotFound();

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

    private async Task<IActionResult> SubmitInternalAsync(SubmitSbomCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var id = await handler.HandleAsync(command, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id }, new { id });
        }
        catch (RequestValidationException ex)
        {
            ModelState.AddModelError(nameof(command.SbomJson), ex.Message);
            return ValidationProblem(ModelState);
        }
    }

    private static bool IsValidJson(string content)
    {
        try
        {
            using var _ = JsonDocument.Parse(content);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static async Task<string> ReadUtf8WithLimitAsync(
        IFormFile file,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        using var memory = new MemoryStream(capacity: (int)Math.Min(file.Length, maxBytes));

        var buffer = new byte[81920];
        long totalRead = 0;

        while (true)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
            if (read == 0)
                break;

            totalRead += read;
            if (totalRead > maxBytes)
                throw new UploadTooLargeException($"Upload exceeded {maxBytes} bytes.");

            await memory.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return Encoding.UTF8.GetString(memory.GetBuffer(), 0, (int)memory.Length);
    }
}
