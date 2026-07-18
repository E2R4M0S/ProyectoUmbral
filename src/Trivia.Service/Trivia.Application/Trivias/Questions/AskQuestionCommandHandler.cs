using System.Collections.Concurrent;
using System.Net.Http.Json;
using MediatR;
using Microsoft.Extensions.Logging;
using Trivia.Application.Common.Interfaces;

namespace Trivia.Application.Trivias.Questions;

public class AskQuestionCommandHandler : IRequestHandler<AskQuestionCommand, Guid>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IQuestionRepository _questionRepo;
    private readonly ILogger<AskQuestionCommandHandler> _logger;

    // Tracks correct answers, answer timings, and team submissions per question
    public static readonly ConcurrentDictionary<Guid, int> CorrectAnswers = new();
    public static readonly ConcurrentDictionary<Guid, List<DateTime>> CorrectAnswerTimestamps = new();
    // (questionId, teamId) → selectedIndex; used for idempotency and cross-device team sync
    public static readonly ConcurrentDictionary<(Guid, Guid), int> TeamAnswers = new();
    // (questionId, teamId) → points awarded; used by the polling endpoint for team sync feedback
    public static readonly ConcurrentDictionary<(Guid, Guid), int> TeamAnswerPoints = new();
    // (questionId, userId) → selectedIndex; one entry per participant for per-user counting
    public static readonly ConcurrentDictionary<(Guid, Guid), int> UserAnswers = new();
    // (sessionId) → current active question data (for HTTP polling fallback when SignalR is unavailable)
    public static readonly ConcurrentDictionary<Guid, CurrentQuestionDto> CurrentQuestions = new();

    public AskQuestionCommandHandler(
        IHttpClientFactory httpClientFactory,
        IQuestionRepository questionRepo,
        ILogger<AskQuestionCommandHandler> logger)
    {
        _httpClientFactory = httpClientFactory;
        _questionRepo = questionRepo;
        _logger = logger;
    }

    public async Task<Guid> Handle(AskQuestionCommand command, CancellationToken ct)
    {
        var questionId = command.QuestionId ?? Guid.NewGuid();
        var askedAt = DateTime.UtcNow;

        // HU-27/HU-29: when this round comes from a saved quiz-bank question, mark it
        // released. Best-effort — a persistence hiccup must not block live gameplay.
        if (command.QuestionId is { } persistedQuestionId)
        {
            try
            {
                await _questionRepo.MarkReleasedAsync(persistedQuestionId, askedAt, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to mark question {QuestionId} as released", persistedQuestionId);
            }
        }

        // Store the correct answer index for later verification
        CorrectAnswers[questionId] = command.CorrectAnswerIndex;
        CorrectAnswerTimestamps[questionId] = new List<DateTime>();

        // Store latest question data per session for HTTP polling fallback (APK via tunnel)
        CurrentQuestions[command.SessionId] = new CurrentQuestionDto(
            questionId, command.QuestionText, command.Options, command.TimeLimitSeconds, askedAt);

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
