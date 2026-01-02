namespace DevHabit.Contracts.Entries;

public sealed record UpdateEntry
{
    public required int Value { get; init; }
    public string? Notes { get; init; }
    public required DateOnly Date { get; init; }
}
