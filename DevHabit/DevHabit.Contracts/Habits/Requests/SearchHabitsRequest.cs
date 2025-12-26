using DevHabit.Contracts.Habits.Enums;

namespace DevHabit.Contracts.Habits.Requests;

public sealed record SearchHabitsRequest : AcceptHeader
{
    public string? Search { get; set; }

    public HabitStatus? Status { get; init; }

    public HabitType? Type { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 10;

    public string Fields { get; set; }
}
