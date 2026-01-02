namespace DevHabit.Contracts.GitHub;

public class StoreGitHubAccessToken
{
    public required string AccessToken { get; init; }
    public required int ExpiresInDays { get; init; }
}
