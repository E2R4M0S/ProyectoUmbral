using FluentValidation;
using Sessions.Application.Sessions.Create;

namespace Sessions.Application.Sessions.Create;

public class CreateSessionCommandValidator : AbstractValidator<CreateSessionCommand>
{
    public CreateSessionCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters");

        RuleFor(x => x.MissionId)
            .NotEmpty().WithMessage("MissionId is required")
            .NotEqual(Guid.Empty).WithMessage("MissionId cannot be empty");
    }
}
