using FluentValidation;

namespace Missions.Application.Operators.Enable;

public class EnableOperatorCommandValidator : AbstractValidator<EnableOperatorCommand>
{
    public EnableOperatorCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}
