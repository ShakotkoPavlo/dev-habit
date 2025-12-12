using System.Data;
using DevHabit.Contracts.Habits.Enums;
using FluentValidation;

namespace DevHabit.Contracts.Habits.Requests;

public sealed record CreateHabitRequest
{
    public required string Name { get; init; }

    public string? Description { get; init; }

    public required Frequency Frequency { get; init; }

    public HabitType Type { get; init; }

    public required Target Target { get; init; }

    public DateOnly? EndDate { get; init; }

    public Milestone? Milestone { get; init; }
}
