using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Sessions.Application.Common;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.Transition.Chain;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;

namespace Sessions.Application.Sessions.Transition;

public class TransitionSessionCommandHandler
    : IRequestHandler<TransitionSessionCommand>
{
    private readonly ISessionRepository _repository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<TransitionSessionCommandHandler> _logger;
    private readonly IEventPublisher _eventPublisher;
    private readonly IStateTransitionHandler _validationChain;

    public TransitionSessionCommandHandler(
        ISessionRepository repository,
        IHttpContextAccessor httpContextAccessor,
        ILogger<TransitionSessionCommandHandler> logger,
        IEventPublisher eventPublisher)
    {
        _repository = repository;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _eventPublisher = eventPublisher;

        // Build the Chain of Responsibility
        var validStatus    = new ValidStatusHandler();
        var notTerminal    = new NotTerminalHandler();
        var hasParticipants = new HasParticipantsHandler();
        validStatus.SetNext(notTerminal).SetNext(hasParticipants);
        _validationChain = validStatus;
    }

    public async Task Handle(TransitionSessionCommand command, CancellationToken ct)
    {
        var session = await _repository.GetByIdAsync(command.Id, ct);
        if (session is null)
        {
            throw new InvalidOperationException($"Session with id '{command.Id}' not found");
        }

        // RB-10: only the operator who created this session may transition it. This is skipped
        // for system-driven transitions (command.SkipOwnershipCheck) — those can run inline
        // inside another user's HTTP request (e.g. a participant's QR scan auto-finishing the
        // session), where HttpContext is NOT null but belongs to someone who isn't the operator.
        if (!command.SkipOwnershipCheck && _httpContextAccessor.HttpContext is not null)
        {
            var currentUserId = CurrentUserClaims.GetUserId(_httpContextAccessor.HttpContext.User);
            if (!session.IsManagedBy(currentUserId))
            {
                throw new UnauthorizedAccessException(
                    "Solo el operador que creó esta sesión puede administrarla");
            }
        }

        // Run the validation chain before transitioning
        _validationChain.Handle(session, command.NewStatus);

        var previousStatus = session.Status;
        var newStatus = Enum.Parse<SessionStatus>(command.NewStatus);
        session.TransitionTo(newStatus);

        await _repository.UpdateAsync(session, ct);

        // RF-15: persisted history of session lifecycle events.
        await _repository.AddAuditEventAsync(SessionAuditEvent.Create(
            session.Id,
            SessionAuditEventTypes.StatusChanged,
            $"Sesión transicionó de '{previousStatus}' a '{session.Status}'"), ct);

        if (session.Status == SessionStatus.Cancelled)
        {
            await _repository.ResetParticipantScoresAsync(session.Id, ct);
        }

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
