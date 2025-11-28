using DevHabit.Domain.Habits.Entities.Enums;

namespace DevHabit.Domain.Habits.Entities;

public sealed class Frequency
{
    public FrequencyType Type { get; set; }

    public int TimesPerPeriod { get; set; }
}