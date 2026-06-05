using Xunit;
using FluentAssertions;
using Trivia.Domain.Entities;

namespace Trivia.Domain.Tests.Entities;

public class QuizTests
{
    [Fact]
    public void DefaultConstruction_ShouldSetDefaultValues()
    {
        var quiz = new Quiz();

        quiz.Title.Should().BeNull();
    }

    [Fact]
    public void Properties_ShouldBeSettableAndGettable()
    {
        var id = Guid.NewGuid();

        var quiz = new Quiz
        {
            Id = id,
            Title = "General Knowledge Trivia"
        };

        quiz.Id.Should().Be(id);
        quiz.Title.Should().Be("General Knowledge Trivia");
    }

    [Fact]
    public void Title_ShouldAllowNull()
    {
        var quiz = new Quiz { Id = Guid.NewGuid() };

        quiz.Title.Should().BeNull();
    }
}