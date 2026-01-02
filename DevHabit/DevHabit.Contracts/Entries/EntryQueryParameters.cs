namespace DevHabit.Contracts.Entries;

public sealed record EntryQueryParameters : AcceptHeader
{
    public string? Fields { get; init; }
}
