using FluentValidation;

namespace DevHabit.Contracts.Entries;

public sealed class CreateEntryBatchValidator : AbstractValidator<CreateEntryBatchRequest>
{
    public CreateEntryBatchValidator(CreateEntryValidator entryValidator)
    {
        RuleFor(x => x.Entries)
            .NotEmpty()
            .WithMessage("At least one entry is required.")
            .Must(entries => entries.Count <= 20)
            .WithMessage("Maximum of 20 entries per batch.");

        RuleForEach(x => x.Entries)
            .SetValidator(entryValidator);
    }
}
