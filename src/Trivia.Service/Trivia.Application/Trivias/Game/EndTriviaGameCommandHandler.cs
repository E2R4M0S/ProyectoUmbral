using System.Net.Http.Json;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Trivia.Application.Trivias.Game;

public class EndTriviaGameCommandHandler : IRequestHandler<EndTriviaGameCommand>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EndTriviaGameCommandHandler> _logger;

    public EndTriviaGameCommandHandler(IHttpClientFactory httpClientFactory, ILogger<EndTriviaGameCommandHandler> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task Handle(EndTriviaGameCommand command, CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient("sessionsService");

        var response = await client.PostAsJsonAsync(
            $"/{command.SessionId}/finish",
            new { },
            ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Sessions API returned {StatusCode} when finishing session {SessionId}: {Body}",
                response.StatusCode, command.SessionId, body);
            throw new InvalidOperationException($"Failed to end trivia game: {response.StatusCode}");
        }

        _logger.LogInformation("Trivia game ended for session {SessionId}", command.SessionId);
    }
}
