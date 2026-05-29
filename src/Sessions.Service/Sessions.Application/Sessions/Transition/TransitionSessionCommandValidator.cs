using FluentValidation;
using Sessions.Application.Sessions.Transition;

namespace Sessions.Application.Sessions.Transition;

public class TransitionSessionCommandValidator : AbstractValidator<TransitionSessionCommand>
{
    private static readonly string[] ValidStatuses =
    {
        "Scheduled", "Preparing", "Active", "Paused", "Finished", "Cancelled"
    };

    public TransitionSessionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required");

        RuleFor(x => x.NewStatus)
            .NotEmpty().WithMessage("NewStatus is required")
            .Must(BeAValidStatus).WithMessage(
                $"NewStatus must be one of: {string.Join(", ", ValidStatuses)}");
    }

    private static bool BeAValidStatus(string status)
        => ValidStatuses.Contains(status);
}