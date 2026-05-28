using FluentValidation;

namespace Teams.Application.Teams.Operators.Disable;

public class DisableOperatorCommandValidator : AbstractValidator<DisableOperatorCommand>
{
    public DisableOperatorCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}