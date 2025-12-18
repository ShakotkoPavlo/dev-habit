using DevHabit.Contracts.Habits.Enums;

namespace DevHabit.Contracts.Habits;

public sealed record Habit : ILinkResponse
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public required Frequency Frequency { get; init; }

    public HabitType Type { get; init; }

    public required Target Target { get; init; }

    public HabitStatus Status { get; init; }

    public bool IsArchived { get; init; }

    public DateOnly? EndDate { get; init; }

    public Milestone? Milestone { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public DateTime? LastCompletedAtUtc { get; init; }

    public List<Link> Links { get; set; }
}
