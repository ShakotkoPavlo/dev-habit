using DevHabit.Contracts.GitHub;
using Refit;

namespace DevHabit.Application.Services;

[Headers("User-Agent: DevHabit/1.0", "Accept: application/vnd.github+json")]
public interface IIGitHubApi
{
    [Get("/user")]
    Task<ApiResponse<GitHubUserProfile?>> GetUserProfileAsync(
        [Authorize(scheme: "Bearer")] string accessToken,
        CancellationToken cancellationToken = default);

    [Get("/users/{username}/events")]
    Task<ApiResponse<List<GitHubEvent>>> GetUserEvents(
        string username,
        [Authorize(scheme: "Bearer")] string accessToken,
        int page = 1,
        [AliasAs("per_page")] int perPage = 100,
        CancellationToken cancellationToken = default);
}
