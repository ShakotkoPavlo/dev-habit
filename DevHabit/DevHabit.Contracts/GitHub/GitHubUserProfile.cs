using DevHabit.Contracts.Habits;
using Newtonsoft.Json;

namespace DevHabit.Contracts.GitHub;

public sealed record GitHubUserProfile(
    [property: JsonProperty("login")] string Login,
    [property: JsonProperty("name")] string? Name,
    [property: JsonProperty("avatar_url")] string AvatarUrl,
    [property: JsonProperty("bio")] string? Bio,
    [property: JsonProperty("public_repos")] int PublicRepos,
    [property: JsonProperty("followers")] int Followers,
    [property: JsonProperty("following")] int Following
) : ILinkResponse
{
    public List<Link> Links { get; set; }
}
