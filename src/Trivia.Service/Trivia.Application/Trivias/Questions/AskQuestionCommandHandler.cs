using System.Collections.Concurrent;
using System.Net.Http.Json;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Trivia.Application.Trivias.Questions;

public class AskQuestionCommandHandler : IRequestHandler<AskQuestionCommand, Guid>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AskQuestionCommandHandler> _logger;

    // Tracks correct answers and answer timings per question
    public static readonly ConcurrentDictionary<Guid, int> CorrectAnswers = new();
    public static readonly ConcurrentDictionary<Guid, List<DateTime>> CorrectAnswerTimestamps = new();

    public AskQuestionCommandHandler(IHttpClientFactory httpClientFactory, ILogger<AskQuestionCommandHandler> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<Guid> Handle(AskQuestionCommand command, CancellationToken ct)
    {
        var questionId = Guid.NewGuid();
        var askedAt = DateTime.UtcNow;

        // Store the correct answer index for later verification
        CorrectAnswers[questionId] = command.CorrectAnswerIndex;
        CorrectAnswerTimestamps[questionId] = new List<DateTime>();

        var client = _httpClientFactory.CreateClient("realTimeHub");
        var response = await client.PostAsJsonAsync("/internal/notifications/question-asked", new
        {
            SessionId = command.SessionId,
            QuestionId = questionId,
            QuestionText = command.QuestionText,
            Options = command.Options,
            TimeLimitSeconds = command.TimeLimitSeconds,
            AskedAt = askedAt
        }, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("RealTimeHub returned {StatusCode} for question-asked", response.StatusCode);
        }

        _logger.LogInformation("Question asked for session {SessionId}: QuestionId={QuestionId}, CorrectAnswer={CorrectIndex}",
            command.SessionId, questionId, command.CorrectAnswerIndex);
        return questionId;
    }
}
