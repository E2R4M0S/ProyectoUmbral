using FluentValidation;
using MediatR;

namespace Sessions.Application.Sessions.Clues;

public record ReleaseClueCommand(Guid SessionId, Guid ClueId, Guid? TeamId = null) : IRequest;

public class ReleaseClueCommandValidator : AbstractValidator<ReleaseClueCommand>
{
    public ReleaseClueCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.ClueId).NotEmpty();
    }
}
