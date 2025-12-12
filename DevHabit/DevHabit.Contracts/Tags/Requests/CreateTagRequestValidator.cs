using FluentValidation;

namespace DevHabit.Contracts.Tags.Requests;

public sealed class CreateTagRequestValidator : AbstractValidator<CreateTagRequest>
{
    public CreateTagRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(3);

        RuleFor(x => x.Description)
            .MaximumLength(50);
    }
}
