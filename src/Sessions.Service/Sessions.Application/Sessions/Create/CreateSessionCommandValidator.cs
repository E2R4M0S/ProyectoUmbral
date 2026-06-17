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

        RuleFor(x => x.Stages)
            .NotNull().WithMessage("Stages list is required")
            .NotEmpty().WithMessage("At least one stage is required");

        RuleForEach(x => x.Stages).ChildRules(stage =>
        {
            stage.RuleFor(s => s.MissionId)
                .NotEmpty().WithMessage("Stage MissionId cannot be empty")
                .NotEqual(Guid.Empty).WithMessage("Stage MissionId cannot be empty");

            stage.RuleFor(s => s.MissionTitle)
                .NotEmpty().WithMessage("Stage MissionTitle is required")
                .MaximumLength(200).WithMessage("Stage MissionTitle must not exceed 200 characters");

            stage.RuleFor(s => s.MissionType)
                .NotEmpty().WithMessage("Stage MissionType is required");

            stage.RuleFor(s => s.Order)
                .GreaterThanOrEqualTo(1).WithMessage("Stage Order must be 1 or greater");
        });

        RuleFor(x => x.Stages)
            .Must(stages => stages is null || stages.Select(s => s.Order).Distinct().Count() == stages.Count)
            .WithMessage("Stage Order values must be unique")
            .When(x => x.Stages is not null && x.Stages.Count > 0);

        RuleFor(x => x.Stages)
            .Must(HaveSequentialOrdersStartingAtOne)
            .WithMessage("Stage Order values must be sequential starting at 1")
            .When(x => x.Stages is not null && x.Stages.Count > 0);
    }

    private static bool HaveSequentialOrdersStartingAtOne(List<StageInput> stages)
    {
        var sorted = stages.Select(s => s.Order).OrderBy(o => o).ToList();
        for (int i = 0; i < sorted.Count; i++)
        {
            if (sorted[i] != i + 1) return false;
        }
        return true;
    }
}
