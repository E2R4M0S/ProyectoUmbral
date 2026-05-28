using FluentValidation;
using Missions.Application.Missions.StatusChange;

namespace Missions.Application.Missions.StatusChange;

public class ChangeMissionStatusCommandValidator : AbstractValidator<ChangeMissionStatusCommand>
{
    private static readonly string[] ValidStatuses = { "Active", "Inactive" };

    public ChangeMissionStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Status is required")
            .Must(BeAValidStatus).WithMessage("Status must be 'Active' or 'Inactive'");
    }

    private static bool BeAValidStatus(string status)
        => ValidStatuses.Contains(status);
}