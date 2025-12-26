using DevHabit.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Api.Extensions;

public static class Extensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication webApplication)
    {
        using IServiceScope scope = webApplication.Services.CreateScope();

        await using ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await using ApplicationIdentityDbContext identityDbContext = scope.ServiceProvider.GetRequiredService<ApplicationIdentityDbContext>();

        try
        {
            await dbContext.Database.MigrateAsync();
            webApplication.Logger.LogInformation("Migration identity applied successfully!");

            await identityDbContext.Database.MigrateAsync();
            webApplication.Logger.LogInformation("Migration applied successfully!");
        }
        catch (Exception e)
        {
            webApplication.Logger.LogError(e, "Migrations not applied!");
            throw;
        }
    }
}
