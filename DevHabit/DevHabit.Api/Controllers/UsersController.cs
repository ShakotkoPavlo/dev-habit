using DevHabit.Api.Mappers;
using DevHabit.Application.Services;
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
public class UsersController(ApplicationDbContext dbContext, UserContext userContext) : ControllerBase
{
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
