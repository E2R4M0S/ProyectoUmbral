using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Trivia.Application.Common.Interfaces;
using Trivia.Application.Trivias.QuizBank;
using Trivia.Domain.Entities;
using Xunit;

namespace Trivia.Application.Tests.QuizBank;

public class CreateQuizCommandHandlerTests
{
    private readonly IQuizRepository _quizRepo = Substitute.For<IQuizRepository>();
    private readonly IQuestionRepository _questionRepo = Substitute.For<IQuestionRepository>();
    private readonly IAnswerRepository _answerRepo = Substitute.For<IAnswerRepository>();
    private readonly ILogger<CreateQuizCommandHandler> _logger = Substitute.For<ILogger<CreateQuizCommandHandler>>();
    private readonly CreateQuizCommandHandler _sut;

    public CreateQuizCommandHandlerTests()
    {
        _sut = new CreateQuizCommandHandler(_quizRepo, _questionRepo, _answerRepo, _logger);
    }

    [Fact]
    public async Task Handle_ShouldCreateQuizWithQuestions()
    {
        var command = new CreateQuizCommand("Test Quiz", new List<QuestionInput>
        {
            new("Pregunta 1", new List<AnswerInput>
            {
                new("Respuesta A", true),
                new("Respuesta B", false)
            })
        });

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        await _quizRepo.Received(1).AddAsync(Arg.Any<Quiz>(), Arg.Any<CancellationToken>());
        await _questionRepo.Received(1).AddRangeAsync(Arg.Any<List<Question>>(), Arg.Any<CancellationToken>());
        await _answerRepo.Received(1).AddRangeAsync(Arg.Any<List<Answer>>(), Arg.Any<CancellationToken>());
    }
}

public class ListQuizzesQueryHandlerTests
{
    private readonly IQuizRepository _repo = Substitute.For<IQuizRepository>();
    private readonly ListQuizzesQueryHandler _sut;

    public ListQuizzesQueryHandlerTests()
    {
        _sut = new ListQuizzesQueryHandler(_repo);
    }

    [Fact]
    public async Task Handle_ShouldReturnQuizzes()
    {
        var quiz = new Quiz { Id = Guid.NewGuid(), Title = "Q1" };
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Quiz> { quiz });
        _repo.GetQuestionsAsync(quiz.Id, Arg.Any<CancellationToken>()).Returns(new List<Question>());

        var result = await _sut.Handle(new ListQuizzesQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Q1");
    }
}

public class GetQuizQueryHandlerTests
{
    private readonly IQuizRepository _repo = Substitute.For<IQuizRepository>();
    private readonly GetQuizQueryHandler _sut;

    public GetQuizQueryHandlerTests()
    {
        _sut = new GetQuizQueryHandler(_repo);
    }

    [Fact]
    public async Task Handle_ShouldReturnQuiz()
    {
        var quizId = Guid.NewGuid();
        _repo.GetByIdAsync(quizId, Arg.Any<CancellationToken>())
            .Returns(new Quiz { Id = quizId, Title = "Test" });
        _repo.GetQuestionsAsync(quizId, Arg.Any<CancellationToken>())
            .Returns(new List<Question>());
        _repo.GetAnswersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<Answer>());

        var result = await _sut.Handle(new GetQuizQuery(quizId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(quizId);
    }

    [Fact]
    public async Task Handle_NotFound_ShouldReturnNull()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Quiz?)null);
        var result = await _sut.Handle(new GetQuizQuery(Guid.NewGuid()), CancellationToken.None);
        result.Should().BeNull();
    }
}
