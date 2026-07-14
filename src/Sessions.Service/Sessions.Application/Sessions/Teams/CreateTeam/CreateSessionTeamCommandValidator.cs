using FluentValidation;

namespace Sessions.Application.Sessions.Teams.CreateTeam;

public class CreateSessionTeamCommandValidator : AbstractValidator<CreateSessionTeamCommand>
{
    public CreateSessionTeamCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del equipo es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre no puede superar los 100 caracteres.");
    }
}
