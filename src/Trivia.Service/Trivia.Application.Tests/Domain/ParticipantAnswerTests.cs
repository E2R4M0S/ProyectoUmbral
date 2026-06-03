using Xunit;
using FluentAssertions;
using Trivia.Domain.Entities;

namespace Trivia.Application.Tests.Domain;

public class ParticipantAnswerTests
{
    [Fact]
    public void Create_ShouldSetProperties()
    {
        var quizId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var answerId = Guid.NewGuid();

        var pa = new ParticipantAnswer
        {
            Id = Guid.NewGuid(),
            QuizId = quizId,
            TeamId = teamId,
            QuestionId = questionId,
            AnswerId = answerId,
            Timestamp = DateTime.UtcNow,
            IsCorrect = true
        };

        pa.QuizId.Should().Be(quizId);
        pa.TeamId.Should().Be(teamId);
        pa.QuestionId.Should().Be(questionId);
        pa.AnswerId.Should().Be(answerId);
        pa.IsCorrect.Should().BeTrue();
    }

    [Fact]
    public void DefaultIsCorrect_ShouldBeFalse()
    {
        var pa = new ParticipantAnswer();
        pa.IsCorrect.Should().BeFalse();
    }
}
