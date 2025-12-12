using DevHabit.Domain.Habits.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevHabit.Infrastructure.Database.EntityConfigurations;

public class HabitTagConfiguration : IEntityTypeConfiguration<HabitTag>
{
    public void Configure(EntityTypeBuilder<HabitTag> builder)
    {
        builder.HasIndex(ht => new { ht.HabitId, ht.TagId });

        builder
            .HasOne<Tag>()
            .WithMany()
            .HasForeignKey(ht => ht.TagId);

        builder
            .HasOne<Habit>()
            .WithMany(h => h.HabitTags)
            .HasForeignKey(ht => ht.HabitId);

    }
}
