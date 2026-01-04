using System.Net.Http.Headers;
using DevHabit.Contracts.GitHub;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Refit;

namespace DevHabit.Application.Services;

public class RefitGitHubService(IIGitHubApi gitHubApi, ILogger<RefitGitHubService> logger)
{
    public async Task<GitHubUserProfile?> GetUserProfileAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        ApiResponse<GitHubUserProfile?> apiResponse = await gitHubApi.GetUserProfileAsync(accessToken, cancellationToken);

        if (!apiResponse.IsSuccessStatusCode)
        {
            logger.LogWarning("Failed to get GitHub user profile. Status code: {StatusCode}", apiResponse.StatusCode);
            return null;
        }

        return apiResponse.Content;
    }

    public async Task<IReadOnlyList<GitHubEvent>?> GetUserEventsAsync(
        string username,
        string accessToken,
        int page = 1,
        int perPage = 100,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(username);

        ApiResponse<List<GitHubEvent>> apiResponse = await gitHubApi.GetUserEvents(username, accessToken, page, perPage, cancellationToken);

        if (!apiResponse.IsSuccessStatusCode)
        {
            logger.LogWarning("Failed to get GitHub user events. Status code: {StatusCode}", apiResponse.StatusCode);

            return null;
        }

        return apiResponse.Content;
    }
}
