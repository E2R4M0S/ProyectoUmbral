using MediatR;
using Microsoft.Extensions.Logging;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.Transition;
using Sessions.Domain.Enums;

namespace Sessions.Application.Sessions.Transition;

public class TransitionSessionCommandHandler
    : IRequestHandler<TransitionSessionCommand>
{
    private readonly ISessionRepository _repository;
    private readonly ILogger<TransitionSessionCommandHandler> _logger;
    private readonly IEventPublisher _publisher;

    public TransitionSessionCommandHandler(
        ISessionRepository repository,
        ILogger<TransitionSessionCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    // Add constructor overload for DI
    public TransitionSessionCommandHandler(ISessionRepository repository, ILogger<TransitionSessionCommandHandler> logger, IEventPublisher publisher)
        : this(repository, logger)
    {
        _publisher = publisher;
    }

    public async Task Handle(TransitionSessionCommand command, CancellationToken ct)
    {
        var session = await _repository.GetByIdAsync(command.Id, ct);
        if (session is null)
        {
            throw new InvalidOperationException($"Session with id '{command.Id}' not found");
        }

        var newStatus = Enum.Parse<SessionStatus>(command.NewStatus);
        session.TransitionTo(newStatus);

        await _repository.UpdateAsync(session, ct);

        _logger.LogInformation(
            "Session status transitioned: Id={SessionId}, Status={Status}",
            session.Id, session.Status);

        // Publish domain event for async processing (optional)
        if (_publisher is not null)
        {
            try
            {
                await _publisher.PublishAsync("SessionStarted", new { SessionId = session.Id, Status = session.Status.ToString(), StartedAt = session.StartedAt }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to publish SessionStarted event for SessionId={SessionId}", session.Id);
            }
        }
    }
}
