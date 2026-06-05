using Xunit;
using FluentAssertions;
using Trivia.Domain.Entities;

namespace Trivia.Domain.Tests.Entities;

public class QuestionTests
{
    [Fact]
    public void DefaultConstruction_ShouldSetDefaultValues()
    {
        var question = new Question();

        question.TimeLimitSeconds.Should().Be(0);
        question.ReleasedAt.Should().BeNull();
    }

    [Fact]
    public void Properties_ShouldBeSettableAndGettable()
    {
        var id = Guid.NewGuid();
        var quizId = Guid.NewGuid();
        var timeLimit = 30;
        var releasedAt = DateTime.UtcNow;

        var question = new Question
        {
            Id = id,
            QuizId = quizId,
            Text = "What is the capital of France?",
            TimeLimitSeconds = timeLimit,
            ReleasedAt = releasedAt
        };

        question.Id.Should().Be(id);
        question.QuizId.Should().Be(quizId);
        question.Text.Should().Be("What is the capital of France?");
        question.TimeLimitSeconds.Should().Be(timeLimit);
        question.ReleasedAt.Should().Be(releasedAt);
    }

    [Fact]
    public void NullableReleasedAt_ShouldAllowNull()
    {
        var question = new Question { Id = Guid.NewGuid(), QuizId = Guid.NewGuid() };

        question.ReleasedAt.Should().BeNull();

        question.ReleasedAt = DateTime.UtcNow;
        question.ReleasedAt.Should().NotBeNull();
    }
}