namespace DevHabit.Contracts.Tags.Requests;

public sealed record UpsertTagsRequest
{
    public IEnumerable<string> Tags { get; set; }
}
