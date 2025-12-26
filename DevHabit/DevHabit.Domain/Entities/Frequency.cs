using DevHabit.Domain.Entities.Enums;

namespace DevHabit.Domain.Entities;

public sealed class Frequency
{
    public FrequencyType Type { get; set; }

    public int TimesPerPeriod { get; set; }
}