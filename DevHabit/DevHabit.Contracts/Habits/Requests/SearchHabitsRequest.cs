using DevHabit.Contracts.Habits.Enums;

namespace DevHabit.Contracts.Habits.Requests;

public class SearchHabitsRequest
{
    public string? Search { get; set; }

    public HabitStatus? Status { get; set; }

    public HabitType? Type { get; set; }
}
