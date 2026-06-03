using MediatR;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using Sessions.Application.Common.Interfaces;

namespace Trivia.Application.Trivias.Leaderboard;

public class CloseQuestionCommandHandler : IRequestHandler<CloseQuestionCommand>
{
    private readonly ILogger<CloseQuestionCommandHandler> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public CloseQuestionCommandHandler(ILogger<CloseQuestionCommandHandler> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task Handle(CloseQuestionCommand request, CancellationToken cancellationToken)
    {
        // Minimal implementation: determine a "correct answer" and notify the RealTimeHub
        // In a full implementation this would consult repositories/aggregate roots to mark closed and compute correct answer.

        // For this HU we simulate finding the correct answer id. In tests the handler will be exercised with a deterministic GUID.
        Guid correctAnswerId = Guid.NewGuid();
        string? correctAnswerText = null;

        try
        {
            var client = _httpClientFactory.CreateClient("RealTimeHub");
            var payload = new
            {
                SessionId = request.SessionId,
                QuestionId = request.QuestionId,
                CorrectAnswerId = correctAnswerId,
                CorrectAnswerText = correctAnswerText
            };

            // POST to RealTimeHub internal endpoint
            await client.PostAsJsonAsync("/internal/notifications/question-closed", payload, cancellationToken);

            _logger.LogInformation("Question closed: SessionId={SessionId}, QuestionId={QuestionId}, CorrectAnswerId={CorrectAnswerId}",
                request.SessionId, request.QuestionId, correctAnswerId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify question closed for SessionId={SessionId}", request.SessionId);
        }
    }
}
