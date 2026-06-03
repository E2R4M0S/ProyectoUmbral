using Xunit;
using FluentAssertions;
using Trivia.Domain.Entities;

namespace Trivia.Application.Tests.Domain;

public class QuizTests
{
    [Fact]
    public void Create_ShouldSetProperties()
    {
        var id = Guid.NewGuid();
        var quiz = new Quiz { Id = id, Title = "Test Quiz" };

        quiz.Id.Should().Be(id);
        quiz.Title.Should().Be("Test Quiz");
    }

    [Fact]
    public void NullTitle_ShouldBeAllowed()
    {
        var quiz = new Quiz { Id = Guid.NewGuid() };
        quiz.Title.Should().BeNull();
    }
}
