using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SbomQualityGate.Api.Models;

public sealed class UploadSbomRequest
{
    [Required]
    public IFormFile? File { get; init; }

    [Required]
    public string Team { get; init; } = string.Empty;

    [Required]
    public string Project { get; init; } = string.Empty;

    [Required]
    public string Version { get; init; } = string.Empty;
}
