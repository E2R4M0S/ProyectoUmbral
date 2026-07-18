namespace Sessions.Application.Common.Interfaces;

/// <summary>
/// Facade Pattern: coordina operaciones de sesion que involucran multiples pasos
/// (cambio de estado + notificacion SignalR). Oculta la complejidad a los endpoints.
/// </summary>
public interface IGameSessionFacade
{
    Task TransitionAndNotify(Guid sessionId, string newStatus, CancellationToken ct = default, bool skipOwnershipCheck = false);
    Task ReleaseClueAndNotify(Guid sessionId, Guid clueId, Guid? teamId, Guid? userId, string? clueContent, int? penalty, CancellationToken ct = default);
    Task NotifyStageAdvanced(Guid sessionId, int newStageOrder, CancellationToken ct = default);
}
