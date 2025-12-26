using DevHabit.Contracts.Habits;

namespace DevHabit.Contracts.Tags;

public record Tag : ILinkResponse
{
    public string Id { get; set; }

    public string Name { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
    public List<Link> Links { get; set; }
}
