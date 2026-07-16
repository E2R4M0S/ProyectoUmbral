namespace Sessions.Application.Common.Interfaces;

public interface IGameNotifier
{
    Task NotifySessionStatusChanged(Guid sessionId, string status, CancellationToken ct = default);
    Task NotifyProgressUpdated(Guid sessionId, object progressData, CancellationToken ct = default);
    Task NotifyClueReleased(Guid sessionId, Guid? teamId, object clueData, CancellationToken ct = default);
    Task NotifyQuestionClosed(Guid sessionId, Guid questionId, Guid correctAnswerId, string? correctAnswerText, CancellationToken ct = default);
    Task NotifyGateOpenedAsync(Guid sessionId, int nextStageIndex, CancellationToken ct = default);
    Task NotifyRankingUpdatedAsync(Guid sessionId, IEnumerable<SessionRankingEntry> entries, CancellationToken ct = default);
    Task NotifyTeamStageAdvancedAsync(Guid sessionId, Guid teamId, int newStageOrder, int totalStages, CancellationToken ct = default);
}
