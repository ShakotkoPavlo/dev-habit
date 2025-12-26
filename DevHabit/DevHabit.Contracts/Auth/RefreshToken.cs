namespace DevHabit.Contracts.Auth;

public sealed record RefreshToken
{
    public required string Value { get; init; }
}
