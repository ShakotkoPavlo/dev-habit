namespace DevHabit.Contracts.Habits.Requests;

public sealed record UpdateMilestoneRequest
{
    public required int Target { get; init; }
}
