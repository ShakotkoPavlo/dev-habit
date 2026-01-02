using DevHabit.Domain.Entities;
using DevHabit.Infrastructure.Database;
using Microsoft.AspNetCore.Identity;
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

    public static async Task SeedInitialDataAsync(this WebApplication webApplication)
    {
        using IServiceScope scope = webApplication.Services.CreateScope();
        RoleManager<IdentityRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        try
        {
            if (!await roleManager.RoleExistsAsync(Roles.AdminRole))
            {
                await roleManager.CreateAsync(new IdentityRole(Roles.AdminRole));
            }

            if (!await roleManager.RoleExistsAsync(Roles.MemberRole))
            {
                await roleManager.CreateAsync(new IdentityRole(Roles.MemberRole));
            }

            webApplication.Logger.LogInformation("Successfully created roles.");
        }
        catch (Exception e)
        {
            webApplication.Logger.LogError(e, "An error occured while seeding data.");
            throw;
        }
    }
}
