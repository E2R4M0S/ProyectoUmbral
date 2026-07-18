using FluentAssertions;
using Trivia.Application.Trivias.QuizBank;
using Xunit;

namespace Trivia.Application.Tests.QuizBank;

public class CreateQuizCommandValidatorTests
{
    private readonly CreateQuizCommandValidator _sut = new();

    private static QuestionInput ValidQuestion(int correctIndex = 0, int answerCount = 2) =>
        new(
            "¿Pregunta?",
            Enumerable.Range(0, answerCount).Select(i => new AnswerInput($"Opción {i}", i == correctIndex)).ToList(),
            30);

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveErrors()
    {
        var command = new CreateQuizCommand("Quiz", new List<QuestionInput> { ValidQuestion() });

        var result = _sut.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithBlankTitle_ShouldHaveError()
    {
        var command = new CreateQuizCommand("   ", new List<QuestionInput> { ValidQuestion() });

        var result = _sut.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithNoCorrectAnswer_ShouldHaveError()
    {
        var question = new QuestionInput("¿Pregunta?", new List<AnswerInput>
        {
            new("A", false),
            new("B", false),
        }, 30);
        var command = new CreateQuizCommand("Quiz", new List<QuestionInput> { question });

        var result = _sut.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Exactly one answer"));
    }

    [Fact]
    public void Validate_WithTwoCorrectAnswers_ShouldHaveError()
    {
        var question = new QuestionInput("¿Pregunta?", new List<AnswerInput>
        {
            new("A", true),
            new("B", true),
        }, 30);
        var command = new CreateQuizCommand("Quiz", new List<QuestionInput> { question });

        var result = _sut.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Exactly one answer"));
    }

    [Fact]
    public void Validate_WithOnlyOneAnswer_ShouldHaveError()
    {
        var question = new QuestionInput("¿Pregunta?", new List<AnswerInput> { new("A", true) }, 30);
        var command = new CreateQuizCommand("Quiz", new List<QuestionInput> { question });

        var result = _sut.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("between 2 and 4 answers"));
    }

    [Fact]
    public void Validate_WithFiveAnswers_ShouldHaveError()
    {
        var question = ValidQuestion(correctIndex: 0, answerCount: 5);
        var command = new CreateQuizCommand("Quiz", new List<QuestionInput> { question });

        var result = _sut.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("between 2 and 4 answers"));
    }

    [Fact]
    public void Validate_WithFourAnswers_ShouldNotHaveError()
    {
        var question = ValidQuestion(correctIndex: 0, answerCount: 4);
        var command = new CreateQuizCommand("Quiz", new List<QuestionInput> { question });

        var result = _sut.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithBlankAnswerText_ShouldHaveError()
    {
        var question = new QuestionInput("¿Pregunta?", new List<AnswerInput>
        {
            new("  ", true),
            new("B", false),
        }, 30);
        var command = new CreateQuizCommand("Quiz", new List<QuestionInput> { question });

        var result = _sut.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("blank"));
    }

    [Fact]
    public void Validate_WithZeroTimeLimit_ShouldHaveError()
    {
        var question = new QuestionInput("¿Pregunta?", new List<AnswerInput>
        {
            new("A", true),
            new("B", false),
        }, 0);
        var command = new CreateQuizCommand("Quiz", new List<QuestionInput> { question });

        var result = _sut.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithEmptyQuestionsList_ShouldHaveError()
    {
        var command = new CreateQuizCommand("Quiz", new List<QuestionInput>());

        var result = _sut.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
