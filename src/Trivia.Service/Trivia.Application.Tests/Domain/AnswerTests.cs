using Xunit;
using FluentAssertions;
using Trivia.Domain.Entities;

namespace Trivia.Application.Tests.Domain;

public class AnswerTests
{
    [Fact]
    public void Create_ShouldSetProperties()
    {
        var id = Guid.NewGuid();
        var questionId = Guid.NewGuid();

        var answer = new Answer
        {
            Id = id,
            QuestionId = questionId,
            Text = "Option A",
            IsCorrect = true
        };

        answer.Id.Should().Be(id);
        answer.QuestionId.Should().Be(questionId);
        answer.Text.Should().Be("Option A");
        answer.IsCorrect.Should().BeTrue();
    }

    [Fact]
    public void DefaultIsCorrect_ShouldBeFalse()
    {
        var answer = new Answer();
        answer.IsCorrect.Should().BeFalse();
    }
}
