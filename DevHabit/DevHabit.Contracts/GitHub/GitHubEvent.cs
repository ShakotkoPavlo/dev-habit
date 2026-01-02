using Newtonsoft.Json;

namespace DevHabit.Contracts.GitHub;

public sealed record GitHubEvent(
    [property: JsonProperty("id")] string Id,
    [property: JsonProperty("type")] string Type,
    [property: JsonProperty("actor")] GitHubActor Actor,
    [property: JsonProperty("repo")] GitHubRepository Repository,
    [property: JsonProperty("payload")] GitHubPayload Payload,
    [property: JsonProperty("public")] bool IsPublic,
    [property: JsonProperty("created_at")] DateTime CreatedAt
);

public sealed record GitHubActor(
    [property: JsonProperty("id")] int Id,
    [property: JsonProperty("login")] string Login,
    [property: JsonProperty("display_login")] string DisplayLogin,
    [property: JsonProperty("avatar_url")] string AvatarUrl
);

public sealed record GitHubRepository(
    [property: JsonProperty("id")] int Id,
    [property: JsonProperty("name")] string Name,
    [property: JsonProperty("url")] string Url
);

public sealed record GitHubPayload(
    [property: JsonProperty("action")] string? Action,
    [property: JsonProperty("ref")] string? Ref,
    [property: JsonProperty("commits")] IReadOnlyList<GitHubCommit>? Commits
);

public sealed record GitHubCommit(
    [property: JsonProperty("sha")] string Sha,
    [property: JsonProperty("message")] string Message,
    [property: JsonProperty("url")] string Url
);
