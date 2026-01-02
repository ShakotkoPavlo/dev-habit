using DevHabit.Contracts.Entries;

namespace DevHabit.Api.Mappers;

public static class EntryMappings
{
    public static Domain.Entities.Entry ToEntity(this CreateEntryRequest request, string userId, Domain.Entities.Habit habit)
    {
        return new Domain.Entities.Entry
        {
            Id = $"e_{Guid.CreateVersion7()}",
            HabitId = request.HabitId,
            UserId = userId,
            Value = request.Value,
            Notes = request.Notes,
            Date = request.Date,
            Source = Enum.Parse<Domain.Entities.Enums.EntrySource>(nameof(EntrySource.Manual)),
            CreatedAtUtc = DateTime.UtcNow,
            Habit = habit
        };
    }

    public static Entry To(this Domain.Entities.Entry entry)
    {
        return new Entry
        {
            Id = entry.Id,
            Habit = new EntryHabit
            {
                Id = entry.HabitId,
                Name = entry.Habit.Name
            },
            Value = entry.Value,
            Notes = entry.Notes,
            Source = Enum.Parse<EntrySource>(entry.Source.ToString()),
            ExternalId = entry.ExternalId,
            IsArchived = entry.IsArchived,
            Date = entry.Date,
            CreatedAtUtc = entry.CreatedAtUtc,
            UpdatedAtUtc = entry.UpdatedAtUtc
        };
    }

    public static void UpdateFrom(this Domain.Entities.Entry entry, UpdateEntry dto)
    {
        entry.Value = dto.Value;
        entry.Notes = dto.Notes;
        entry.Date = dto.Date;
        entry.UpdatedAtUtc = DateTime.UtcNow;
    }
}
