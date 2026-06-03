namespace Sessions.Application.Common.Interfaces;

public interface IGameNotifier
{
    Task NotifySessionStatusChanged(Guid sessionId, string status, CancellationToken ct = default);
    Task NotifyProgressUpdated(Guid sessionId, object progressData, CancellationToken ct = default);
    Task NotifyClueReleased(Guid sessionId, Guid? teamId, object clueData, CancellationToken ct = default);
    Task NotifyQuestionClosed(Guid sessionId, Guid questionId, Guid correctAnswerId, string? correctAnswerText, CancellationToken ct = default);
}
