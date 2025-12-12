using DevHabit.Contracts.Habits.Enums;
using FluentValidation;

namespace DevHabit.Contracts.Habits.Requests;

public sealed record CreateHabitRequest
{
    public required string Name { get; init; }

    public string? Description { get; init; }

    public required Frequency Frequency { get; init; }

    public HabitType Type { get; init; }

    public required Target Target { get; init; }

    public DateOnly? EndDate { get; init; }

    public Milestone? Milestone { get; init; }
}

public sealed class CreateHabitRequestValidator : AbstractValidator<CreateHabitRequest>
{
    private static readonly string[] AllowedUnits = ["hours", "minutes", "times", "pages", "words", "books", "cal", "km"];

    private static readonly string[] AllowedUnitsForBinaryHabits

    public CreateHabitRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
        RuleFor(x => x.Description)
            .MaximumLength(500);
        RuleFor(x => x.Frequency)
            .NotNull()
            .SetValidator(new FrequencyValidator());
        RuleFor(x => x.Target)
            .NotNull()
            .SetValidator(new TargetValidator());
        When(x => x.Milestone is not null, () =>
        {
            RuleFor(x => x.Milestone)
                .SetValidator(new MilestoneValidator());
        });
    }
}
