using System.Net.Http.Json;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Trivia.Application.Trivias.Ranking;

public class UpdateRankingCommandHandler : IRequestHandler<UpdateRankingCommand, List<RankingEntryDto>>
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UpdateRankingCommandHandler> _logger;

    public UpdateRankingCommandHandler(IHttpClientFactory httpClientFactory, ILogger<UpdateRankingCommandHandler> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<List<RankingEntryDto>> Handle(UpdateRankingCommand command, CancellationToken ct)
    {
        var ranking = new List<RankingEntryDto>
        {
            new(1, "Equipo A", 120),
            new(2, "Equipo B", 90),
            new(3, "Jugador 1", 75),
            new(4, "Equipo C", 60),
        };

        var client = _httpClientFactory.CreateClient("realTimeHub");
        var payload = new
        {
            SessionId = command.SessionId,
            Ranking = ranking
        };

        var response = await client.PostAsJsonAsync("/internal/notifications/ranking-updated", payload, ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("RealTimeHub returned {StatusCode} for ranking-updated notification", response.StatusCode);
        }

        _logger.LogInformation("Ranking updated for session {SessionId}", command.SessionId);
        return ranking;
    }
}
