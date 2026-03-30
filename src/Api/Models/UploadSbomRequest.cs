using System.ComponentModel.DataAnnotations;

namespace SbomQualityGate.Api.Models;

public sealed class UploadSbomRequest
{
    [Required]
    public IFormFile? File { get; init; }

    [Required]
    public Guid ProductId { get; init; }

    [Required]
    public string Version { get; init; } = string.Empty;
}
