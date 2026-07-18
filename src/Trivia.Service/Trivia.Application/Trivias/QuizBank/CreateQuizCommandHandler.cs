using MediatR;
using Microsoft.Extensions.Logging;
using Trivia.Application.Common.Interfaces;
using Trivia.Domain.Entities;

namespace Trivia.Application.Trivias.QuizBank;

public class CreateQuizCommandHandler : IRequestHandler<CreateQuizCommand, Guid>
{
    private readonly IQuizRepository _quizRepo;
    private readonly IQuestionRepository _questionRepo;
    private readonly IAnswerRepository _answerRepo;
    private readonly ILogger<CreateQuizCommandHandler> _logger;

    public CreateQuizCommandHandler(
        IQuizRepository quizRepo,
        IQuestionRepository questionRepo,
        IAnswerRepository answerRepo,
        ILogger<CreateQuizCommandHandler> logger)
    {
        _quizRepo = quizRepo;
        _questionRepo = questionRepo;
        _answerRepo = answerRepo;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateQuizCommand command, CancellationToken ct)
    {
        var quiz = new Quiz
        {
            Id = Guid.NewGuid(),
            Title = command.Title
        };
        await _quizRepo.AddAsync(quiz, ct);

        var questions = new List<Question>();
        var allAnswers = new List<Answer>();

        foreach (var q in command.Questions)
        {
            var questionId = Guid.NewGuid();
            questions.Add(new Question
            {
                Id = questionId,
                QuizId = quiz.Id,
                Text = q.Text,
                TimeLimitSeconds = q.TimeLimitSeconds
            });

            foreach (var a in q.Answers)
            {
                allAnswers.Add(new Answer
                {
                    Id = Guid.NewGuid(),
                    QuestionId = questionId,
                    Text = a.Text,
                    IsCorrect = a.IsCorrect
                });
            }
        }

        await _questionRepo.AddRangeAsync(questions, ct);
        await _answerRepo.AddRangeAsync(allAnswers, ct);

        _logger.LogInformation("Quiz created: Id={QuizId}, Title={Title}, Questions={Count}",
            quiz.Id, quiz.Title, command.Questions.Count);

        return quiz.Id;
    }
}
