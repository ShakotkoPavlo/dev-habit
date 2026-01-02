using System.Linq.Expressions;
using DevHabit.Contracts.Entries;

namespace DevHabit.Api.Mappers;

public static class EntryQueries
{
    public static Expression<Func<DevHabit.Domain.Entities.Entry, Entry>> ProjectTo()
    {
        return entry => new Entry
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
}
