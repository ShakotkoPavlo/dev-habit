using System.Linq.Expressions;
using DevHabit.Contracts.Habits;
using DevHabit.Domain.Habits.Entities.Enums;
using DomainHabit = DevHabit.Domain.Habits.Entities.Habit;
using DomainFrequency = DevHabit.Domain.Habits.Entities.Frequency;
using DomainMilestone = DevHabit.Domain.Habits.Entities.Milestone;
using DomainTarget = DevHabit.Domain.Habits.Entities.Target;
using DevHabit.Contracts.Habits.Requests;

namespace DevHabit.Api.Mappers;

public static class HabitMappings
{
    public static DomainHabit ToEntity(this CreateHabitRequest request)
    {
        return new DomainHabit
        {
            Id = $"h_{Guid.CreateVersion7()}",
            Name = request.Name,
            Description = request.Description,
            Frequency = new DomainFrequency
            {
                Type = Enum.Parse<FrequencyType>(request.Frequency.Type.ToString()),
                TimesPerPeriod = request.Frequency.TimesPerPeriod
            },
            Type = HabitType.Binary,
            Target = new DomainTarget
            {
                Value = request.Target.Value,
                Unit = request.Target.Unit
            },
            Status = HabitStatus.Ongoing,
            IsArchived = false,
            EndDate = request.EndDate,
            Milestone = request.Milestone is not null
            ? new DomainMilestone
            {
                Current = 0,
                Target = request.Milestone.Target
            }
            : null,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public static Habit ToContract(this DomainHabit habit)
    {
        return new Habit
        {
            Id = habit.Id,
            Name = string.Empty,
            Description = habit.Description,
            Frequency = new Frequency
            {
                Type = Enum.Parse<Contracts.Habits.Enums.FrequencyType>(habit.Frequency.Type.ToString()),
                TimesPerPeriod = habit.Frequency.TimesPerPeriod
            },
            Type = Enum.Parse<Contracts.Habits.Enums.HabitType>(habit.Type.ToString()),
            Target = new Target
            {
                Value = habit.Target.Value,
                Unit = habit.Target.Unit
            },
            Status = Enum.Parse<Contracts.Habits.Enums.HabitStatus>(habit.Status.ToString()),
            IsArchived = habit.IsArchived,
            EndDate = habit.EndDate,
            Milestone = habit.Milestone is not null
                ? new Milestone
                {
                    Current = 0,
                    Target = habit.Milestone.Target
                }
                : null,
            CreatedAtUtc = habit.CreatedAtUtc,
            LastCompletedAtUtc = habit.LastCompletedAtUtc,
            UpdatedAtUtc = habit.UpdatedAtUtc,
        };
    }

    public static void UpdateFromContract(this DomainHabit habit, UpdateHabitRequest request)
    {
        habit.Name = request.Name;
        habit.Description = request.Description;
        habit.Type = Enum.Parse<HabitType>(request.Type.ToString());

        habit.Frequency = new DomainFrequency
        {
            Type = Enum.Parse<FrequencyType>(request.Frequency.Type.ToString()),
            TimesPerPeriod = request.Frequency.TimesPerPeriod
        };

        habit.Target = new DomainTarget
        {
            Value = request.Target.Value,
            Unit = request.Target.Unit
        };

        if (request.Milestone is not null)
        {
            habit.Milestone ??= new DomainMilestone();
            habit.Milestone.Target = request.Milestone.Target;
        }

        habit.UpdatedAtUtc = DateTime.UtcNow;
    }
}

public static class HabitQueries
{
    public static Expression<Func<DomainHabit, Habit>> ProjectToContract()
    {
        return h => new Habit
        {
            Id = h.Id,
            Name = h.Name,
            Description = h.Description,
            Frequency = new Frequency
            {
                Type = Enum.Parse<Contracts.Habits.Enums.FrequencyType>(h.Frequency.Type.ToString()),
                TimesPerPeriod = h.Frequency.TimesPerPeriod
            },
            Type = Enum.Parse<Contracts.Habits.Enums.HabitType>(h.Type.ToString()),
            Target = new Target
            {
                Value = h.Target.Value,
                Unit = h.Target.Unit
            },
            Status = Enum.Parse<Contracts.Habits.Enums.HabitStatus>(h.Status.ToString()),
            IsArchived = h.IsArchived,
            EndDate = h.EndDate,
            Milestone = h.Milestone != null
                ? new Milestone
                {
                    Target = h.Milestone.Target,
                    Current = h.Milestone.Current
                }
                : null,
            CreatedAtUtc = h.CreatedAtUtc,
            UpdatedAtUtc = h.UpdatedAtUtc,
            LastCompletedAtUtc = h.LastCompletedAtUtc,
            //Tags = h.Tags.Select(t => t.Id).ToArray()
        };
    }
}
