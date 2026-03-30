namespace SbomQualityGate.Domain.Entities;

public class Product
{
    public Guid Id { get; init; }

    public Guid TeamId { get; init; }

    public string Name { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    public Team Team { get; init; } = null!;

    public ICollection<Sbom> Sboms { get; init; } = [];
}
