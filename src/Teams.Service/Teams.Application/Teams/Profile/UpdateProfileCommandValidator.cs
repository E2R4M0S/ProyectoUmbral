using FluentValidation;

namespace Teams.Application.Teams.Profile;

public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Alias)
            .NotEmpty().WithMessage("Alias is required")
            .Length(3, 50).WithMessage("Alias must be between 3 and 50 characters")
            .Matches(@"^[a-zA-Z0-9_]+$").WithMessage("Alias must be alphanumeric with underscores only");
    }
}