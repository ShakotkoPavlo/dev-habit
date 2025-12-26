namespace DevHabit.Contracts.Auth;

public sealed record RegisterUser
{
    public required string Name { get; init; }

    public required string Email { get; init; }

    public required string Password { get; init; }

    public required string ConfirmationPassword { get; init; }
}
