using MediatR;
using Microsoft.Extensions.Logging;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.Transition.Chain;
using Sessions.Domain.Enums;

namespace Sessions.Application.Sessions.Transition;

public class TransitionSessionCommandHandler
    : IRequestHandler<TransitionSessionCommand>
{
    private readonly ISessionRepository _repository;
    private readonly ILogger<TransitionSessionCommandHandler> _logger;
    private readonly IEventPublisher _eventPublisher;
    private readonly IStateTransitionHandler _validationChain;

    public TransitionSessionCommandHandler(
        ISessionRepository repository,
        ILogger<TransitionSessionCommandHandler> logger,
        IEventPublisher eventPublisher)
    {
        _repository = repository;
        _logger = logger;
        _eventPublisher = eventPublisher;

        // Build the Chain of Responsibility
        var validStatus = new ValidStatusHandler();
        var notTerminal = new NotTerminalHandler();
        validStatus.SetNext(notTerminal);
        _validationChain = validStatus;
    }

    public async Task Handle(TransitionSessionCommand command, CancellationToken ct)
    {
        var session = await _repository.GetByIdAsync(command.Id, ct);
        if (session is null)
        {
            throw new InvalidOperationException($"Session with id '{command.Id}' not found");
        }

        // Run the validation chain before transitioning
        _validationChain.Handle(session, command.NewStatus);

        var newStatus = Enum.Parse<SessionStatus>(command.NewStatus);
        session.TransitionTo(newStatus);

        await _repository.UpdateAsync(session, ct);

        _logger.LogInformation(
            "Session status transitioned: Id={SessionId}, Status={Status}",
            session.Id, session.Status);

        if (session.Status == SessionStatus.Finished)
        {
            try
            {
                await _eventPublisher.PublishAsync("session.status.changed", new
                {
                    SessionId = session.Id,
                    Status = session.Status.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Event publish failed for session {SessionId}", session.Id);
            }
        }
    }
}
