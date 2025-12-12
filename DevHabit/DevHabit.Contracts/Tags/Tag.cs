namespace DevHabit.Contracts.Tags;

public record Tag
{
    public string Id { get; set; }

    public string Name { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }
}
