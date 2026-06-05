using Xunit;
using FluentAssertions;
using Trivia.Domain.Entities;

namespace Trivia.Domain.Tests.Entities;

public class ParticipantAnswerTests
{
    [Fact]
    public void DefaultConstruction_ShouldSetDefaultValues()
    {
        var participantAnswer = new ParticipantAnswer();

        participantAnswer.Timestamp.Should().Be(default);
        participantAnswer.IsCorrect.Should().BeFalse();
    }

    [Fact]
    public void Properties_ShouldBeSettableAndGettable()
    {
        var id = Guid.NewGuid();
        var quizId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var answerId = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;

        var participantAnswer = new ParticipantAnswer
        {
            Id = id,
            QuizId = quizId,
            TeamId = teamId,
            QuestionId = questionId,
            AnswerId = answerId,
            Timestamp = timestamp,
            IsCorrect = true
        };

        participantAnswer.Id.Should().Be(id);
        participantAnswer.QuizId.Should().Be(quizId);
        participantAnswer.TeamId.Should().Be(teamId);
        participantAnswer.QuestionId.Should().Be(questionId);
        participantAnswer.AnswerId.Should().Be(answerId);
        participantAnswer.Timestamp.Should().Be(timestamp);
        participantAnswer.IsCorrect.Should().BeTrue();
    }
}