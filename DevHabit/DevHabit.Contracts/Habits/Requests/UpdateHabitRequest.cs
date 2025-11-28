using DevHabit.Contracts.Habits.Enums;

namespace DevHabit.Contracts.Habits.Requests;

public sealed record UpdateHabitRequest
{
    public required string Name { get; init; }

    public string? Description { get; init; }

    public required Frequency Frequency { get; init; }

    public HabitType Type { get; init; }

    public required Target Target { get; init; }

    public DateOnly? EndDate { get; init; }

    public UpdateMilestoneRequest? Milestone { get; init; }
}
