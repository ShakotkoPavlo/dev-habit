using DevHabit.Contracts.Habits.Enums;

namespace DevHabit.Contracts.Habits;

public sealed record HabitWithTagsV2
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

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }

    public DateTime? LastCompletedAt { get; init; }

    public required string[] Tags { get; set; }
}
