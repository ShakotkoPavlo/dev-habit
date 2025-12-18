namespace DevHabit.Contracts.Habits;

public sealed class Link
{
    public required string Href { get; init; }

    public required string Rel { get; init; }

    public required string Method { get; init; }
}
