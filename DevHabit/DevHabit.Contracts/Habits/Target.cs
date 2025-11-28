namespace DevHabit.Contracts.Habits;

public sealed class Target
{
    public int Value { get; init; }

    public required string Unit { get; init; }
}