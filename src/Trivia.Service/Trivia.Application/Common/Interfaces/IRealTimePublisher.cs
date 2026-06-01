namespace Trivia.Application.Common.Interfaces;

public interface IRealTimePublisher
{
    Task PublishAsync(string eventName, object payload, CancellationToken ct = default);
}
