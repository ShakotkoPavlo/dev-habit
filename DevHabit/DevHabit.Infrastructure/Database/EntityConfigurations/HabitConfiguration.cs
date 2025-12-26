using DevHabit.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevHabit.Infrastructure.Database.EntityConfigurations;

public sealed class HabitConfiguration : IEntityTypeConfiguration<Habit>
{
    public void Configure(EntityTypeBuilder<Habit> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id).HasMaxLength(500);
        builder.Property(h => h.UserId).HasMaxLength(500);
        builder.Property(h => h.Name).HasMaxLength(100);
        builder.Property(h => h.Description).HasMaxLength(500);

        builder.OwnsOne(h => h.Frequency);

        builder.OwnsOne(h => h.Target, target =>
        {
            target.Property(t => t.Unit).HasMaxLength(100);
        });

        builder.OwnsOne(h => h.Milestone);

        builder
            .HasMany(ht => ht.Tags)
            .WithMany()
            .UsingEntity<HabitTag>();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(h => h.UserId);
    }
}
