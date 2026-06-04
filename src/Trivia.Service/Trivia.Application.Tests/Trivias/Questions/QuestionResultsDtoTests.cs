using Xunit;
using FluentAssertions;
using Trivia.Application.Trivias.Questions;

namespace Trivia.Application.Tests.Trivias.Questions;

public class QuestionResultsDtoTests
{
    [Fact]
    public void Create_ShouldSetProperties()
    {
        var quizId = System.Guid.NewGuid();
        var questionId = System.Guid.NewGuid();
        var results = new System.Collections.Generic.List<AnswerResultDto>
        {
            new(System.Guid.NewGuid(), "Option A", 5, 50.0)
        };

        var dto = new QuestionResultsDto(quizId, questionId, results);

        dto.QuizId.Should().Be(quizId);
        dto.QuestionId.Should().Be(questionId);
        dto.Results.Should().HaveCount(1);
        dto.Results[0].Text.Should().Be("Option A");
        dto.Results[0].Count.Should().Be(5);
        dto.Results[0].Percentage.Should().Be(50.0);
    }

    [Fact]
    public void AnswerResultDto_Properties()
    {
        var dto = new AnswerResultDto(System.Guid.NewGuid(), "B", 3, 30.0);

        dto.Text.Should().Be("B");
        dto.Count.Should().Be(3);
        dto.Percentage.Should().Be(30.0);
    }
}
