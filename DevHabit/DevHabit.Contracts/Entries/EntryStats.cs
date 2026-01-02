namespace DevHabit.Contracts.Entries;

public sealed record EntryStats
{
    public required List<DailyStats> DailyStats { get; init; }
    public required int TotalEntries { get; init; }
    public required int CurrentStreak { get; init; }
    public required int LongestStreak { get; init; }
}
