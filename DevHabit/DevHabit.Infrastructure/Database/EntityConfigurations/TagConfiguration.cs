using DevHabit.Domain.Habits.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevHabit.Infrastructure.Database.EntityConfigurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id).HasMaxLength(500);
        builder.Property(h => h.Name).HasMaxLength(50);
        builder.Property(h => h.Description).HasMaxLength(500);
        builder.HasIndex(h => new { h.Name }).IsUnique();
    }
}
