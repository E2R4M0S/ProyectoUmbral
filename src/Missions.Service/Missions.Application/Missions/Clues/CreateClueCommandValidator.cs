using FluentValidation;
using Missions.Application.Missions.Clues;
using Missions.Domain.Enums;

namespace Missions.Application.Missions.Clues;

public class CreateClueCommandValidator : AbstractValidator<CreateClueCommand>
{
    public CreateClueCommandValidator()
    {
        RuleFor(x => x.MissionId)
            .NotEmpty().WithMessage("MissionId is required");

        RuleFor(x => x.StageId)
            .NotEmpty().WithMessage("StageId is required");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required")
            .MaximumLength(2000).WithMessage("Content must not exceed 2000 characters");

        RuleFor(x => x.Penalty)
            .GreaterThanOrEqualTo(0).WithMessage("Penalty must be non-negative")
            .When(x => x.Penalty.HasValue);

        RuleFor(x => x.ReleaseType)
            .NotEmpty().WithMessage("ReleaseType is required")
            .Must(BeAValidReleaseType).WithMessage("ReleaseType must be 'Auto' or 'Manual'");
    }

    private static bool BeAValidReleaseType(string releaseType)
    {
        return Enum.TryParse<ReleaseType>(releaseType, out _);
    }
}
