using FluentValidation;
using Missions.Application.Missions.Clues;

namespace Missions.Application.Missions.Clues;

public class DeleteClueCommandValidator : AbstractValidator<DeleteClueCommand>
{
    public DeleteClueCommandValidator()
    {
        RuleFor(x => x.MissionId)
            .NotEmpty().WithMessage("MissionId is required");

        RuleFor(x => x.StageId)
            .NotEmpty().WithMessage("StageId is required");

        RuleFor(x => x.ClueId)
            .NotEmpty().WithMessage("ClueId is required");
    }
}
