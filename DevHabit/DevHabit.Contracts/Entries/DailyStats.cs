namespace DevHabit.Contracts.Entries;

public sealed record DailyStats
{
    public required DateOnly Date { get; init; }
    public required int Count { get; init; }
}
