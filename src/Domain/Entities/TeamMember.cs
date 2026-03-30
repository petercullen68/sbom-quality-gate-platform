namespace SbomQualityGate.Domain.Entities;

public class TeamMember
{
    public Guid UserId { get; init; }

    public Guid TeamId { get; init; }

    public DateTime JoinedAt { get; init; }

    public Team Team { get; init; } = null!;
}
