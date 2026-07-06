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

    [Fact]
    public async Task Handle_WithMultipleQuestions_PersistsAllAnswers()
    {
        var command = new CreateQuizCommand("Multi Quiz", new List<QuestionInput>
        {
            new("Pregunta 1", new List<AnswerInput> { new("A", true), new("B", false) }),
            new("Pregunta 2", new List<AnswerInput> { new("X", false), new("Y", true), new("Z", false) }),
        });

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        await _questionRepo.Received(1).AddRangeAsync(
            Arg.Is<List<Question>>(q => q.Count == 2), Arg.Any<CancellationToken>());
        await _answerRepo.Received(1).AddRangeAsync(
            Arg.Is<List<Answer>>(a => a.Count == 5), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithEmptyQuestions_PersistsQuizOnly()
    {
        var command = new CreateQuizCommand("Empty Quiz", new List<QuestionInput>());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        await _quizRepo.Received(1).AddAsync(Arg.Any<Quiz>(), Arg.Any<CancellationToken>());
        await _questionRepo.Received(1).AddRangeAsync(
            Arg.Is<List<Question>>(q => q.Count == 0), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReturnsNewQuizId()
    {
        var command = new CreateQuizCommand("My Quiz", new List<QuestionInput>());

        var id1 = await _sut.Handle(command, CancellationToken.None);
        var id2 = await _sut.Handle(command, CancellationToken.None);

        id1.Should().NotBe(id2);
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

    [Fact]
    public async Task Handle_EmptyRepo_ReturnsEmptyList()
    {
        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Quiz>());

        var result = await _sut.Handle(new ListQuizzesQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsCorrectQuestionCount()
    {
        var quizId = Guid.NewGuid();
        _repo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Quiz> { new() { Id = quizId, Title = "Quiz con preguntas" } });
        _repo.GetQuestionsAsync(quizId, Arg.Any<CancellationToken>())
            .Returns(new List<Question>
            {
                new() { Id = Guid.NewGuid(), QuizId = quizId, Text = "P1" },
                new() { Id = Guid.NewGuid(), QuizId = quizId, Text = "P2" },
                new() { Id = Guid.NewGuid(), QuizId = quizId, Text = "P3" },
            });

        var result = await _sut.Handle(new ListQuizzesQuery(), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].QuestionCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_MultipleQuizzes_ReturnsAll()
    {
        var quizzes = Enumerable.Range(1, 3)
            .Select(i => new Quiz { Id = Guid.NewGuid(), Title = $"Quiz {i}" })
            .ToList();

        _repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(quizzes);
        foreach (var q in quizzes)
            _repo.GetQuestionsAsync(q.Id, Arg.Any<CancellationToken>()).Returns(new List<Question>());

        var result = await _sut.Handle(new ListQuizzesQuery(), CancellationToken.None);

        result.Should().HaveCount(3);
        result.Select(r => r.Title).Should().Contain(["Quiz 1", "Quiz 2", "Quiz 3"]);
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

    [Fact]
    public async Task Handle_WithQuestionsAndAnswers_ReturnsFullDetail()
    {
        var quizId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var question = new Question { Id = questionId, QuizId = quizId, Text = "¿Pregunta?", TimeLimitSeconds = 20 };
        var answers = new List<Answer>
        {
            new() { Id = Guid.NewGuid(), QuestionId = questionId, Text = "Opción A", IsCorrect = true },
            new() { Id = Guid.NewGuid(), QuestionId = questionId, Text = "Opción B", IsCorrect = false },
        };

        _repo.GetByIdAsync(quizId, Arg.Any<CancellationToken>())
            .Returns(new Quiz { Id = quizId, Title = "Full Quiz" });
        _repo.GetQuestionsAsync(quizId, Arg.Any<CancellationToken>())
            .Returns(new List<Question> { question });
        _repo.GetAnswersAsync(questionId, Arg.Any<CancellationToken>())
            .Returns(answers);

        var result = await _sut.Handle(new GetQuizQuery(quizId), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Questions.Should().HaveCount(1);
        result.Questions[0].Answers.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldQueryAnswersForEachQuestion()
    {
        var quizId = Guid.NewGuid();
        var q1 = Guid.NewGuid();
        var q2 = Guid.NewGuid();

        _repo.GetByIdAsync(quizId, Arg.Any<CancellationToken>())
            .Returns(new Quiz { Id = quizId, Title = "Multi Q" });
        _repo.GetQuestionsAsync(quizId, Arg.Any<CancellationToken>())
            .Returns(new List<Question>
            {
                new() { Id = q1, QuizId = quizId, Text = "P1" },
                new() { Id = q2, QuizId = quizId, Text = "P2" },
            });
        _repo.GetAnswersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<Answer>());

        await _sut.Handle(new GetQuizQuery(quizId), CancellationToken.None);

        await _repo.Received(1).GetAnswersAsync(q1, Arg.Any<CancellationToken>());
        await _repo.Received(1).GetAnswersAsync(q2, Arg.Any<CancellationToken>());
    }
}
