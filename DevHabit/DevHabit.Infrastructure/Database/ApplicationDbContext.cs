using DevHabit.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Infrastructure.Database;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Habit> Habits { get; set; }

    public DbSet<Tag> Tags { get; set; }

    public DbSet<HabitTag> HabitTags { get; set; }

    public DbSet<Entry> Entries { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<GitHubAccessToken> GitHubAccessTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DatabaseConstants.ApplicationSchema);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
