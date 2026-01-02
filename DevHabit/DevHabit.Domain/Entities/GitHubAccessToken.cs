namespace DevHabit.Domain.Entities;

public class GitHubAccessToken
{
    public string Id { get; set; }

    public required string Token { get; set; }

    public required string UserId { get; set; }

    public required DateTime ExpiredAtUtc { get; set; }

    public required DateTime CreatedAtUtc { get; set; }
}
