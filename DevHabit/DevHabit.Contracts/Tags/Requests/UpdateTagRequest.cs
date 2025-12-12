namespace DevHabit.Contracts.Tags.Requests;

public sealed record UpdateTagRequest
{
    public required string Name { get; set; }

    public string? Description { get; set; }
}
