using FluentValidation;
using Missions.Application.Missions.Create;
using Missions.Domain.Enums;

namespace Missions.Application.Missions.Create;

public class CreateMissionCommandValidator : AbstractValidator<CreateMissionCommand>
{
    private static readonly int[] ValidTimeMinutes = { -1, 15, 30, 60, 90 };

    public CreateMissionCommandValidator()
    {
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
            .Must(t => ValidTimeMinutes.Contains(t))
            .WithMessage("TimeMinutes must be one of: -1 (10s test), 15, 30, 60, 90")
            .When(x => x.Type != "Trivia");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Type is required")
            .Must(BeAValidMissionType).WithMessage("Type must be Treasure or Trivia");
    }

    private static bool BeAValidDifficulty(string difficulty)
    {
        return Enum.TryParse<Difficulty>(difficulty, out _);
    }

    private static bool BeAValidMissionType(string type)
    {
        return Enum.TryParse<MissionType>(type, out _);
    }
}