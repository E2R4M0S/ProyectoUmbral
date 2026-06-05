using Xunit;
using FluentAssertions;
using Trivia.Domain.Entities;

namespace Trivia.Domain.Tests.Entities;

public class AnswerTests
{
    [Fact]
    public void Constructor_ShouldSetDefaultValues()
    {
        var answer = new Answer();

        answer.Id.Should().Be(Guid.Empty);
        answer.QuestionId.Should().Be(Guid.Empty);
        answer.Text.Should().BeNull();
        answer.IsCorrect.Should().BeFalse();
    }

    [Fact]
    public void Properties_ShouldBeSettableAndGettable()
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
    public void TwoAnswersWithSameId_ShouldBeEqual()
    {
        var id = Guid.NewGuid();

        var answer1 = new Answer { Id = id, Text = "Answer 1" };
        var answer2 = new Answer { Id = id, Text = "Answer 2" };

        answer1.Should().BeEquivalentTo(answer2, options => options.Excluding(a => a.Text));
        answer1.Id.Should().Be(answer2.Id);
    }
}