using DevHabit.Contracts.Habits.Enums;

namespace DevHabit.Contracts.Habits;

public sealed class Frequency
{
    public FrequencyType Type { get; init; }

    public int TimesPerPeriod { get; init; }
}
