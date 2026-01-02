using System.Threading;
using DevHabit.Application.Services;
using DevHabit.Contracts;
using DevHabit.Contracts.GitHub;
using DevHabit.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DevHabit.Api.Controllers;

[ApiController]
[Authorize(Roles = Roles.MemberRole)]
[Route("github")]
public class GitHubController(GitHubAccessTokenService gitHubAccessTokenService, GitHubService gitHubService, UserContext userContext, LinkService linkService) : ControllerBase
{
    [HttpPut("personal-access-token")]
    public async Task<IActionResult> StoreAccessToken(StoreGitHubAccessToken gitHubAccessToken, CancellationToken cancellationToken = default)
    {
        string? userId = await userContext.GetUserIdAsync(cancellationToken);

        if (userId == null)
        {
            return Unauthorized();
        }

        await gitHubAccessTokenService.StoreAsync(userId, gitHubAccessToken, cancellationToken);

        return NoContent();
    }

    [HttpDelete("personal-access-token")]
    public async Task<IActionResult> RevokeAccessToken(CancellationToken cancellationToken = default)
    {
        string? userId = await userContext.GetUserIdAsync(cancellationToken);

        if (userId == null)
        {
            return Unauthorized();
        }

        await gitHubAccessTokenService.RevokeAsync(userId, cancellationToken);

        return NoContent();
    }

    [HttpGet("profile")]
    public async Task<ActionResult<GitHubUserProfile>> GetUserProfile([FromHeader]AcceptHeader header, CancellationToken cancellationToken = default)
    {
        string? userId = await userContext.GetUserIdAsync(cancellationToken);

        if (userId == null)
        {
            return Unauthorized();
        }

        string? accessToken = await gitHubAccessTokenService.GetAsync(userId, cancellationToken);

        if (accessToken == null)
        {
            return NotFound();
        }

        GitHubUserProfile? userProfile = await gitHubService.GetUserProfileAsync(accessToken, cancellationToken);

        if (userProfile == null)
        {
            return NotFound();
        }

        if (header.IncludeLinks)
        {
            userProfile.Links =
            [
                linkService.GenerateLink(nameof(GetUserProfile), "self", HttpMethods.Get),
                linkService.GenerateLink(nameof(StoreAccessToken), "store-token", HttpMethods.Put),
                linkService.GenerateLink(nameof(RevokeAccessToken), "revoke-token", HttpMethods.Delete),
            ];
        }

        return Ok(userProfile);
    }
}
