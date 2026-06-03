using Xunit;
using FluentAssertions;
using Trivia.Domain.Entities;

namespace Trivia.Application.Tests.Domain;

public class QuestionTests
{
    [Fact]
    public void Create_ShouldSetProperties()
    {
        var id = Guid.NewGuid();
        var quizId = Guid.NewGuid();

        var q = new Question
        {
            Id = id,
            QuizId = quizId,
            TimeLimitSeconds = 30,
            ReleasedAt = null
        };

        q.Id.Should().Be(id);
        q.QuizId.Should().Be(quizId);
        q.TimeLimitSeconds.Should().Be(30);
        q.ReleasedAt.Should().BeNull();
    }

    [Fact]
    public void Release_ShouldSetReleasedAt()
    {
        var q = new Question { Id = Guid.NewGuid(), QuizId = Guid.NewGuid() };
        var now = DateTime.UtcNow;
        q.ReleasedAt = now;
        q.ReleasedAt.Should().Be(now);
    }

    [Fact]
    public void DefaultTimeLimitSeconds_ShouldBeZero()
    {
        var q = new Question();
        q.TimeLimitSeconds.Should().Be(0);
    }
}
