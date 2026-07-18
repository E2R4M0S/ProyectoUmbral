using FluentValidation;

namespace Trivia.Application.Trivias.QuizBank;

public class CreateQuizCommandValidator : AbstractValidator<CreateQuizCommand>
{
    public CreateQuizCommandValidator()
    {
        RuleFor(c => c.Title).NotEmpty();
        RuleFor(c => c.Questions).NotEmpty();

        RuleForEach(c => c.Questions).SetValidator(new QuestionInputValidator());
    }
}

public class QuestionInputValidator : AbstractValidator<QuestionInput>
{
    public QuestionInputValidator()
    {
        RuleFor(q => q.Text).NotEmpty();
        RuleFor(q => q.TimeLimitSeconds).GreaterThan(0);

        // HU-28: 2-4 answers per question, none blank, exactly one marked correct.
        RuleFor(q => q.Answers)
            .Must(answers => answers.Count is >= 2 and <= 4)
            .WithMessage("Each question must have between 2 and 4 answers.");

        RuleForEach(q => q.Answers)
            .ChildRules(answer => answer.RuleFor(a => a.Text).NotEmpty().WithMessage("Answer text cannot be blank."));

        RuleFor(q => q.Answers)
            .Must(answers => answers.Count(a => a.IsCorrect) == 1)
            .WithMessage("Exactly one answer must be marked as correct.");
    }
}
