using FluentValidation;
using Missions.Application.Missions.Update;
using Missions.Domain.Enums;

namespace Missions.Application.Missions.Update;

public class UpdateMissionCommandValidator : AbstractValidator<UpdateMissionCommand>
{
    private static readonly int[] ValidTimeMinutes = { 15, 30, 60, 90 };

    public UpdateMissionCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Id is required");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters");

        RuleFor(x => x.Difficulty)
            .NotEmpty().WithMessage("Difficulty is required")
            .Must(BeAValidDifficulty).WithMessage("Difficulty must be Easy, Medium, or Hard");

        RuleFor(x => x.TimeMinutes)
            .NotEmpty().WithMessage("TimeMinutes is required")
            .Must(t => ValidTimeMinutes.Contains(t))
            .WithMessage("TimeMinutes must be one of: 15, 30, 60, 90");
    }

    private static bool BeAValidDifficulty(string difficulty)
    {
        return Enum.TryParse<Difficulty>(difficulty, out _);
    }
}
