using DevHabit.Api.Mappers;
using DevHabit.Api.Providers;
using DevHabit.Contracts.Auth;
using DevHabit.Domain.Entities;
using DevHabit.Infrastructure.Database;
using DevHabit.Infrastructure.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using DomainRefreshToken = DevHabit.Domain.Entities.RefreshToken;
using RefreshToken = DevHabit.Contracts.Auth.RefreshToken;

namespace DevHabit.Api.Controllers;

[ApiController]
[Route("[controller]")]
[AllowAnonymous]
public class AuthenticationController(
    UserManager<IdentityUser> userManager,
    ApplicationIdentityDbContext identityDbContext,
    ApplicationDbContext dbContext,
    TokenProvider tokenProvider,
    IOptions<JwtAuthOptions> options) : ControllerBase
{
    private readonly JwtAuthOptions _jwtAuthOptions = options.Value;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterUser registerUser)
    {
        await using IDbContextTransaction transaction = await identityDbContext.Database.BeginTransactionAsync();
        dbContext.Database.SetDbConnection(identityDbContext.Database.GetDbConnection());
        await dbContext.Database.UseTransactionAsync(transaction.GetDbTransaction());

        var identityUser = new IdentityUser
        {
            Email = registerUser.Email,
            UserName = registerUser.Name
        };

        IdentityResult identityResult = await userManager.CreateAsync(identityUser, registerUser.Password);

        if (!identityResult.Succeeded)
        {
            var extensions = new Dictionary<string, object?>
            {
                { "errors", identityResult.Errors.ToDictionary(e => e.Code, e => e.Description) }
            };

            return Problem(
                detail: "Unable to register user, please try again",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: extensions);
        }

        User user = registerUser.ToEntity();
        user.IdentityId = identityUser.Id;

        dbContext.Users.Add(user);

        await dbContext.SaveChangesAsync();

        AccessToken accessToken = tokenProvider.Create(new TokenRequest(identityUser.Id, identityUser.Email));

        var refreshToken = new DomainRefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = identityUser.Id,
            Token = accessToken.RefreshToken,
            User = identityUser,
            ExpiredAtUtc = DateTime.UtcNow.AddDays(_jwtAuthOptions.RefreshTokenExpirationDays)
        };

        identityDbContext.RefreshTokens.Add(refreshToken);

        await identityDbContext.SaveChangesAsync();

        await transaction.CommitAsync();

        return Ok(accessToken);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AccessToken>> Login(LoginUser loginUser)
    {
        IdentityUser? identityUser = await userManager.FindByEmailAsync(loginUser.Email);

        if (identityUser is null || !await userManager.CheckPasswordAsync(identityUser, loginUser.Password))
        {
            return Unauthorized();
        }

        var tokenRequest = new TokenRequest(identityUser.Id, identityUser.Email!);

        AccessToken accessToken = tokenProvider.Create(tokenRequest);

        return Ok(accessToken);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AccessToken>> Refresh(RefreshToken refreshToken)
    {
        DomainRefreshToken token = await identityDbContext
            .RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == refreshToken.Value);

        if (token is null || token.ExpiredAtUtc < DateTime.UtcNow)
        {
            return Unauthorized();
        }

        var tokenRequest = new TokenRequest(token.UserId, token.User.Email!);

        AccessToken accessToken = tokenProvider.Create(tokenRequest);

        token.Token = accessToken.RefreshToken;
        token.ExpiredAtUtc = DateTime.UtcNow.AddDays(_jwtAuthOptions.RefreshTokenExpirationDays);

        await identityDbContext.SaveChangesAsync();

        return Ok(accessToken);
    }
}
