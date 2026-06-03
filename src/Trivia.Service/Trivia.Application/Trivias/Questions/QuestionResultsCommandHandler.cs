using MediatR;
using Microsoft.Extensions.Logging;
using Trivia.Application.Common.Interfaces;

namespace Trivia.Application.Trivias.Questions;

public class QuestionResultsCommandHandler : IRequestHandler<QuestionResultsCommand>
{
    private readonly IAnswerRepository _answerRepo;
    private readonly IParticipantAnswerRepository _participantRepo;
    private readonly IEventPublisher _publisher;
    private readonly ILogger<QuestionResultsCommandHandler> _logger;

    public QuestionResultsCommandHandler(IAnswerRepository answerRepo, IParticipantAnswerRepository participantRepo, IEventPublisher publisher, ILogger<QuestionResultsCommandHandler> logger)
    {
        _answerRepo = answerRepo;
        _participantRepo = participantRepo;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Handle(QuestionResultsCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Calculating results for QuizId={QuizId} QuestionId={QuestionId}", request.QuizId, request.QuestionId);

        // Load possible answers for the question
        var answers = await _answerRepo.GetByQuestionIdAsync(request.QuestionId, ct);
        var participantAnswers = await _participantRepo.GetByQuizAsync(request.QuizId, ct);

        // Filter participant answers for this question
        var filtered = participantAnswers.Where(pa => pa.QuestionId == request.QuestionId).ToList();
        var total = filtered.Count;

        var results = new List<AnswerResultDto>();
        foreach (var a in answers)
        {
            var count = filtered.Count(f => f.AnswerId == a.Id);
            var percentage = total == 0 ? 0.0 : (double)count * 100.0 / total;
            results.Add(new AnswerResultDto(a.Id, a.Text, count, Math.Round(percentage, 2)));
        }

        var dto = new QuestionResultsDto(request.QuizId, request.QuestionId, results);

        // Publish via real-time hub
        await _publisher.PublishAsync("QuestionResultsUpdated", dto, ct);
    }
}
