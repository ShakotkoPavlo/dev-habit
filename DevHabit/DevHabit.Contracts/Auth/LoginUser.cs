namespace DevHabit.Contracts.Auth;

public sealed record LoginUser
{
    public required string Email { get; init; }

    public required string Password { get; init; }
}
