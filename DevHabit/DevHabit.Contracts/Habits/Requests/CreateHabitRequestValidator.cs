using DevHabit.Contracts.Habits.Enums;
using FluentValidation;

namespace DevHabit.Contracts.Habits.Requests;

public sealed class CreateHabitRequestValidator : AbstractValidator<CreateHabitRequest>
{
    private static readonly string[] AllowedUnits = ["hours", "minutes", "times", "pages", "words", "books", "cal", "km"];

    private static readonly string[] AllowedUnitsForBinaryHabits = ["sessions", "tasks"];

    public CreateHabitRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(100)
            .WithMessage("Habit name must be between 3 and 100 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .When(x => x.Description is not null)
            .WithMessage("Habit description must be max 500 characters.");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Invalid habit type.");

        RuleFor(x => x.Frequency.Type)
            .IsInEnum()
            .WithMessage("Invalid frequency type.");

        RuleFor(x => x.Frequency.TimesPerPeriod)
            .GreaterThan(0)
            .WithMessage("Invalid frequency period, must be greater then 0.");

        RuleFor(x => x.Target.Unit)
            .NotNull()
            .Must(unit => AllowedUnits.Contains(unit.ToLowerInvariant()))
            .WithMessage($"Unit must be one of : {string.Join(", ", AllowedUnits)}");

        RuleFor(x => x.Target.Value)
            .GreaterThan(0)
            .WithMessage("Target value must be greater than 0");

        RuleFor(x => x.EndDate)
            .Must(date => date is null || date.Value > DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("End date must be in the future.");

        When(x => x.Milestone is not null, () =>
        {
            RuleFor(x => x.Milestone!.Target)
                .GreaterThan(0)
                .WithMessage("Milestone target must be greater than 0");
        });

        RuleFor(x => x.Target.Unit)
            .Must((request, unit) => IsTargetUnitCompatibleWithType(request.Type, unit))
            .WithMessage("Target unit is not compatible with the habit type.");
    }

    private static bool IsTargetUnitCompatibleWithType(HabitType habitType, string unit)
    {
        string normalizerUnit = unit.ToLowerInvariant();

        return habitType switch
        {
            HabitType.Binary => AllowedUnitsForBinaryHabits.Contains(normalizerUnit),
            HabitType.Measurable => AllowedUnits.Contains(normalizerUnit),
            _ => false,
        };
    }
}
