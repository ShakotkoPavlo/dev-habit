using DevHabit.Infrastructure.Database;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DevHabit.Application.Services;

public sealed class UserContext(
    IHttpContextAccessor contextAccessor,
    ApplicationDbContext applicationDbContext,
    IMemoryCache memoryCache)
{
    private const string CacheKeyPrefix = "users:id";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public async Task<string?> GetUserIdAsync(CancellationToken cancellationToken = default)
    {
        string? identityId = contextAccessor.HttpContext?.User.GetIdentityId();

        if (identityId is null)
        {
            return null;
        }

        string cacheKey = $"{CacheKeyPrefix}{identityId}";

        string? userId = await memoryCache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SetAbsoluteExpiration(CacheDuration);

            string? userId = await applicationDbContext.Users
                .Where(u => u.IdentityId == identityId)
                .Select(u => u.Id)
                .FirstOrDefaultAsync(cancellationToken);

            return userId;
        });

        return userId;
    }
}
