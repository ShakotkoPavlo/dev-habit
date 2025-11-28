using DevHabit.Domain.Habits.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevHabit.Infrastructure.Database;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Habit> Habits { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DatabaseConstants.ApplicationSchema);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
