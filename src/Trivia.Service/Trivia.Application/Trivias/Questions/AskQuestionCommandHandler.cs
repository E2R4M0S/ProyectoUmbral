using System.Net.Http.Json;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Trivia.Application.Trivias.Questions;

public class AskQuestionCommandHandler : IRequestHandler<AskQuestionCommand>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AskQuestionCommandHandler> _logger;

    public AskQuestionCommandHandler(IHttpClientFactory httpClientFactory, ILogger<AskQuestionCommandHandler> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task Handle(AskQuestionCommand command, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("realTimeHub");
        var response = await client.PostAsJsonAsync("/internal/notifications/question-asked", new
        {
            SessionId = command.SessionId,
            QuestionId = Guid.NewGuid(),
            QuestionText = command.QuestionText,
            Options = command.Options,
            TimeLimitSeconds = command.TimeLimitSeconds
        }, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("RealTimeHub returned {StatusCode} for question-asked", response.StatusCode);
        }

        _logger.LogInformation("Question asked for session {SessionId}", command.SessionId);
    }
}
