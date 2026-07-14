using MediatR;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.Transition;

namespace Sessions.Infrastructure.Services;

/// <summary>
/// Facade Pattern: coordina operaciones de sesion multi-paso.
/// Encapsula la logica de transicion + notificacion que antes estaba duplicada en cada endpoint.
/// </summary>
public class GameSessionFacade : IGameSessionFacade
{
    private readonly IMediator _mediator;
    private readonly IGameNotifier _notifier;

    public GameSessionFacade(IMediator mediator, IGameNotifier notifier)
    {
        _mediator = mediator;
        _notifier = notifier;
    }

    public async Task TransitionAndNotify(Guid sessionId, string newStatus, CancellationToken ct = default)
    {
        var command = new TransitionSessionCommand(sessionId, newStatus);
        await _mediator.Send(command, ct);
        await _notifier.NotifySessionStatusChanged(sessionId, newStatus, ct);
    }

    public async Task ReleaseClueAndNotify(Guid sessionId, Guid clueId, Guid? teamId, string? clueContent, int? penalty, CancellationToken ct = default)
    {
        var clueData = new
        {
            ClueId = clueId,
            Text = clueContent ?? "Pista liberada por el operador",
            Penalty = penalty,
            ReleasedAt = DateTime.UtcNow
        };

        await _notifier.NotifyClueReleased(sessionId, teamId, clueData, ct);
    }

    public async Task NotifyStageAdvanced(Guid sessionId, int newStageOrder, CancellationToken ct = default)
    {
        await _notifier.NotifyProgressUpdated(sessionId, new
        {
            stageAdvanced = true,
            currentStageOrder = newStageOrder,
            elapsedSeconds = 0
        }, ct);
    }
}
