using DevHabit.Contracts.GitHub;
using DevHabit.Domain.Entities;
using DevHabit.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Application.Services;

public sealed class GitHubAccessTokenService(ApplicationDbContext dbContext, EncryptionService encryptionService)
{
    public async Task StoreAsync(
        string userId,
        StoreGitHubAccessToken accessToken,
        CancellationToken token = default)
    {
        GitHubAccessToken? existingToken = await GetAccessTokenAsync(userId, token);

        if (existingToken != null)
        {
            existingToken.Token = encryptionService.Encrypt(accessToken.AccessToken);
            existingToken.ExpiredAtUtc = DateTime.UtcNow.AddDays(accessToken.ExpiresInDays);
        }
        else
        {
            dbContext.GitHubAccessTokens.Add(new GitHubAccessToken
            {
                Id = $"gh_{Guid.CreateVersion7()}",
                UserId = userId,
                Token = encryptionService.Encrypt(accessToken.AccessToken),
                CreatedAtUtc = DateTime.UtcNow,
                ExpiredAtUtc = DateTime.UtcNow.AddDays(accessToken.ExpiresInDays)
            });
        }

        await dbContext.SaveChangesAsync(token);
    }


    public async Task<string?> GetAsync(string userId, CancellationToken token = default)
    {
        GitHubAccessToken? gitHubAccessToken = await GetAccessTokenAsync(userId, token);

        return encryptionService.Decrypt(gitHubAccessToken.Token);
    }

    public async Task RevokeAsync(string userId, CancellationToken token = default)
    {
        GitHubAccessToken? gitHubAccessToken = await GetAccessTokenAsync(userId, token);

        if (gitHubAccessToken is null)
        {
            return;
        }

        dbContext.GitHubAccessTokens.Remove(gitHubAccessToken);

        await dbContext.SaveChangesAsync(token);
    }

    private async Task<GitHubAccessToken> GetAccessTokenAsync(string userId, CancellationToken token)
    {
        return await dbContext.GitHubAccessTokens.SingleOrDefaultAsync(p => p.UserId == userId, token);
    }
}
