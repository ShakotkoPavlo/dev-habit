using System.Diagnostics;
using DevHabit.Api.Mappers;
using DevHabit.Contracts.User;
using DevHabit.Infrastructure.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class UsersController(ApplicationDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<User>> GetUserById(string id)
    {
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
}
