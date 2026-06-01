using System.Text.Json;
using Microsoft.Extensions.Logging;
using Trivia.Application.Common.Interfaces;

namespace Trivia.Infrastructure.Messaging;

/// <summary>
/// Minimal HTTP-based publisher that forwards trivia domain events to the RealTimeHub service.
/// This keeps the Trivia service decoupled while allowing real-time notifications without introducing
/// a message broker at this stage.
/// </summary>
public class HttpEventPublisher : IEventPublisher, Trivia.Application.Common.Interfaces.IRealTimePublisher
{
    private readonly HttpClient _client;
    private readonly ILogger<HttpEventPublisher> _logger;

    public HttpEventPublisher(HttpClient client, ILogger<HttpEventPublisher> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task PublishAsync(string eventName, object payload, CancellationToken ct = default)
    {
        var path = eventName switch
        {
            "TriviaStarted" => "/internal/notifications/session-status",
            "ProgressUpdated" => "/internal/notifications/progress",
            "ClueReleased" => "/internal/notifications/clue-released",
            _ => $"/internal/events/{eventName}"
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        try
        {
            var resp = await _client.PostAsync(path, content, ct);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Event publisher received non-success response {Status} for {Event}", resp.StatusCode, eventName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish event {Event}", eventName);
        }
    }
}
