using System.Net.Http.Json;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Trivia.Application.Trivias.Questions;

public class AskQuestionCommandHandler : IRequestHandler<AskQuestionCommand, Guid>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AskQuestionCommandHandler> _logger;

    public AskQuestionCommandHandler(IHttpClientFactory httpClientFactory, ILogger<AskQuestionCommandHandler> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<Guid> Handle(AskQuestionCommand command, CancellationToken ct)
    {
        var questionId = Guid.NewGuid();
        var client = _httpClientFactory.CreateClient("realTimeHub");
        var response = await client.PostAsJsonAsync("/internal/notifications/question-asked", new
        {
            SessionId = command.SessionId,
            QuestionId = questionId,
            QuestionText = command.QuestionText,
            Options = command.Options,
            TimeLimitSeconds = command.TimeLimitSeconds
        }, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("RealTimeHub returned {StatusCode} for question-asked", response.StatusCode);
        }

        _logger.LogInformation("Question asked for session {SessionId}: QuestionId={QuestionId}", command.SessionId, questionId);
        return questionId;
    }
}
