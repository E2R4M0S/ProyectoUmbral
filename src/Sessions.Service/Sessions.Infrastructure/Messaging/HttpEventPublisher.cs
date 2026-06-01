using Sessions.Application.Common.Interfaces;
using System.Text.Json;

namespace Sessions.Infrastructure.Messaging;

/// <summary>
/// Small HTTP-based event publisher that forwards domain events to the RealTimeHub service.
/// It maps a small set of known events to the RealTimeHub internal notification endpoints.
/// This keeps the Sessions service decoupled and avoids coupling to a message broker for now.
/// </summary>
public class HttpEventPublisher : IEventPublisher
{
    private readonly HttpClient _client;

    public HttpEventPublisher(HttpClient client)
    {
        _client = client;
    }

    public async Task PublishAsync(string eventName, object payload, CancellationToken ct = default)
    {
        // Map logical event names to RealTimeHub endpoints
        var path = eventName switch
        {
            "SessionStarted" => "/internal/notifications/session-status",
            "ProgressUpdated" => "/internal/notifications/progress",
            "ClueReleased" => "/internal/notifications/clue-released",
            _ => $"/internal/events/{eventName}"
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        // Fire-and-forget style: attempt to post and ignore non-fatal errors to not break the command flow
        try
        {
            var resp = await _client.PostAsync(path, content, ct);
            // swallow failures; caller already logs
        }
        catch
        {
            // Intentionally ignore exceptions here; caller handles reliability/logging
        }
    }
}
