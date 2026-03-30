namespace SbomQualityGate.Domain.Entities;

public class Team
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }

    public ICollection<Product> Products { get; init; } = [];

    public ICollection<TeamMember> Members { get; init; } = [];
}
