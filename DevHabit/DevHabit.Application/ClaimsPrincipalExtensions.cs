using System.Security.Claims;

namespace DevHabit.Application;

public static class ClaimsPrincipalExtensions
{
    public static string? GetIdentityId(this ClaimsPrincipal? claimsPrincipal)
    {
        string? identity = claimsPrincipal?.FindFirstValue(ClaimTypes.NameIdentifier);

        return identity;
    }
}
