using MediatR;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using Trivia.Application.Trivias.Questions;

namespace Trivia.Application.Trivias.Leaderboard;

public class CloseQuestionCommandHandler : IRequestHandler<CloseQuestionCommand>
{
    private readonly ILogger<CloseQuestionCommandHandler> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMediator _mediator;

    public CloseQuestionCommandHandler(ILogger<CloseQuestionCommandHandler> logger, IHttpClientFactory httpClientFactory, IMediator mediator)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _mediator = mediator;
    }

    public async Task Handle(CloseQuestionCommand request, CancellationToken cancellationToken)
    {
        // Remove from HTTP polling fallback (question is no longer active)
        AskQuestionCommandHandler.CurrentQuestions.TryRemove(request.SessionId, out _);

        // Resolve correct answer from in-memory store populated by AskQuestionCommandHandler
        var correctIndex = AskQuestionCommandHandler.CorrectAnswers.GetValueOrDefault(request.QuestionId, 0);
        var correctAnswerId = Guid.Parse($"00000000-0000-0000-0000-00000000000{correctIndex}");

        try
        {
            var client = _httpClientFactory.CreateClient("RealTimeHub");
            var payload = new
            {
                SessionId = request.SessionId,
                QuestionId = request.QuestionId,
                CorrectAnswerId = correctAnswerId,
                CorrectAnswerText = (string?)null
            };
            await client.PostAsJsonAsync("/internal/notifications/question-closed", payload, cancellationToken);

            _logger.LogInformation("Question closed: SessionId={SessionId}, QuestionId={QuestionId}, CorrectIndex={CorrectIndex}",
                request.SessionId, request.QuestionId, correctIndex);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to notify question closed for SessionId={SessionId}", request.SessionId);
        }

        // Broadcast per-option answer breakdown (HU-43)
        try
        {
            await _mediator.Send(new QuestionResultsCommand(request.SessionId, request.QuestionId), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to compute question results for SessionId={SessionId}", request.SessionId);
        }
    }
}
