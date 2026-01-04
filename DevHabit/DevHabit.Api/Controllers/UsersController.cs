using System.Net.Mime;
using DevHabit.Api.Mappers;
using DevHabit.Application.Services;
using DevHabit.Contracts;
using DevHabit.Domain.Entities;
using DevHabit.Infrastructure.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using User = DevHabit.Contracts.User.User;

namespace DevHabit.Api.Controllers;

[ApiController]
[Authorize(Roles = $"{Roles.MemberRole}")]
[Route("[controller]")]
[Produces(
    MediaTypeNames.Application.Json,
    CustomMediaTypesNames.Application.JsonV1,
    CustomMediaTypesNames.Application.HateoasJson,
    CustomMediaTypesNames.Application.HateoasJsonV1,
    CustomMediaTypesNames.Application.HateoasJsonV2)]
[ProducesResponseType<User>(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class UsersController(ApplicationDbContext dbContext, UserContext userContext) : ControllerBase
{
    /// <summary>
    /// Retrieves the user details for the specified user identifier if the caller is authorized and the identifier
    /// matches the caller's own user ID.
    /// </summary>
    /// <remarks>This method requires the caller to have the Admin role and only allows access to their own
    /// user information. If the caller's user ID cannot be determined, the request is unauthorized. If the specified ID
    /// does not match the caller's user ID, access is forbidden.</remarks>
    /// <param name="id">The unique identifier of the user to retrieve. Must match the caller's user ID; otherwise, access is forbidden.</param>
    /// <returns>An <see cref="ActionResult{User}"/> containing the user details if found and authorized; otherwise, an
    /// appropriate HTTP response such as Unauthorized, Forbid, or NotFound.</returns>
    [Authorize(Roles = $"{Roles.AdminRole}")]
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUserById(string id)
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        if (id != userId)
        {
            return Forbid();
        }

        User? user = await dbContext.Users
            .Where(u => u.Id == id)
            .Select(UserQueries.ProjectToContract())
            .FirstOrDefaultAsync();

        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    /// <summary>
    /// Retrieves the current authenticated user's profile information.
    /// </summary>
    /// <remarks>This endpoint requires the caller to be authenticated. The returned user profile is projected
    /// using the contract defined in <see cref="UserQueries.ProjectToContract"/>. If the user is not authenticated or
    /// their profile cannot be found, the appropriate HTTP status code is returned.</remarks>
    /// <returns>An <see cref="ActionResult{User}"/> containing the current user's profile if found; returns <see
    /// cref="UnauthorizedResult"/> if the user is not authenticated, or <see cref="NotFoundResult"/> if the user
    /// profile does not exist.</returns>
    [HttpGet("me")]
    public async Task<ActionResult<User>> GetCurrentUser()
    {
        string? userId = await userContext.GetUserIdAsync();

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        User? user = await dbContext.Users
            .Where(u => u.IdentityId == userId)
            .Select(UserQueries.ProjectToContract())
            .FirstOrDefaultAsync();

        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }
}
