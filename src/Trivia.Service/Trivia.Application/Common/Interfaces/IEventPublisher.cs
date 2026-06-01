namespace Trivia.Application.Common.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync(string eventName, object payload, CancellationToken ct = default);
}
