using Microsoft.Extensions.Logging;
using Sessions.Application.Common.Interfaces;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;

namespace Sessions.Application.Sessions.Timeout;

// RF-02: the client-side countdown was purely decorative — nothing forced a mission/session
// forward once its declared TimeMinutes elapsed. This enforces it server-side: once the
// current mission's time is up, auto-advance past its remaining stages (or finish the
// session if it was the last mission), mirroring what the operator's manual controls do.
public class SessionTimeoutEnforcementService
{
    private const int UnsetTimeMinutesDefaultSeconds = 600;

    // RF-07: clues marked ReleaseType.Automatic are released one at a time, every 5 minutes of
    // mission time, in the order Missions.Service returns them for the stage — independent of
    // the mission's declared total duration (a short mission can still cycle through several
    // clues before SessionTimeoutEnforcementService ends it).
    private static readonly TimeSpan AutomaticClueReleaseInterval = TimeSpan.FromMinutes(5);

    private readonly ISessionRepository _repository;
    private readonly IGameSessionFacade _facade;
    private readonly IGameNotifier _notifier;
    private readonly IMissionCatalogService _missionCatalogService;
    private readonly ILogger<SessionTimeoutEnforcementService> _logger;

    public SessionTimeoutEnforcementService(
        ISessionRepository repository,
        IGameSessionFacade facade,
        IGameNotifier notifier,
        IMissionCatalogService missionCatalogService,
        ILogger<SessionTimeoutEnforcementService> logger)
    {
        _repository = repository;
        _facade = facade;
        _notifier = notifier;
        _missionCatalogService = missionCatalogService;
        _logger = logger;
    }

    public async Task EnforceActiveSessionsAsync(CancellationToken ct = default)
    {
        var (activeSessions, _) = await _repository.GetSessionsAsync(
            search: null, status: "Active", missionId: null, page: 1, pageSize: 1000, ct);

        foreach (var summary in activeSessions)
        {
            try
            {
                await EnforceSessionTimeoutAsync(summary.Id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enforce timeout for session {SessionId}", summary.Id);
            }

            try
            {
                await EnforceAutomaticClueReleaseAsync(summary.Id, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enforce automatic clue release for session {SessionId}", summary.Id);
            }
        }
    }

    // RF-07: the operator's manual "release clue" button already exists (ReleaseClueEndpoint);
    // this covers the other half — clues authored with ReleaseType.Automatic in Missions.Service
    // that must surface on their own once the team has spent enough time on the stage, without
    // waiting for the operator to click anything.
    public async Task EnforceAutomaticClueReleaseAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _repository.GetByIdWithStagesAsync(sessionId, ct);
        if (session is null || session.Status != SessionStatus.Active || !session.CurrentMissionStartedAt.HasValue)
            return;

        var currentStage = session.GetCurrentStage();
        if (currentStage is null || currentStage.MissionType != "Treasure")
            return; // Clues only exist for Treasure stages (mirrors ReleaseClueEndpoint).

        var elapsedSeconds = (DateTime.UtcNow - session.CurrentMissionStartedAt.Value).TotalSeconds;
        var dueCount = (int)(elapsedSeconds / AutomaticClueReleaseInterval.TotalSeconds);
        if (dueCount <= 0) return; // less than 5 minutes into the stage — nothing due yet

        var automaticClues = await _missionCatalogService.GetAutomaticCluesAsync(
            currentStage.MissionId, currentStage.MissionStageId, ct);
        if (automaticClues.Count == 0) return;

        var auditTrail = await _repository.GetAuditTrailAsync(sessionId, ct);
        var alreadyReleasedClueIds = auditTrail
            .Where(e => e.EventType == SessionAuditEventTypes.AutomaticClueReleased)
            .Select(e => e.ClueId)
            .ToHashSet();

        // Only the clues whose 5-minute mark has been reached (1st at 5min, 2nd at 10min, ...),
        // and only those not already released — one call may cover more than one tick if the
        // enforcer missed a run (e.g. after a restart).
        var cluesToRelease = automaticClues
            .Take(Math.Min(dueCount, automaticClues.Count))
            .Where(clue => !alreadyReleasedClueIds.Contains(clue.Id));

        foreach (var clue in cluesToRelease)
        {
            await _facade.ReleaseClueAndNotify(sessionId, clue.Id, teamId: null, userId: null, clue.Content, clue.Penalty, ct);

            await _repository.AddAuditEventAsync(SessionAuditEvent.Create(
                sessionId,
                SessionAuditEventTypes.AutomaticClueReleased,
                $"Pista automática liberada para la etapa '{currentStage.StageName}': {clue.Content}",
                clueId: clue.Id), ct);

            if (clue.Penalty is { } penalty && penalty > 0)
            {
                await _repository.ApplyCluePenaltyAsync(
                    sessionId, teamId: null, userId: null, penalty,
                    $"Pista automática liberada: {clue.Content}", ct);

                var rankingAfterPenalty = await _repository.GetSessionRankingAsync(sessionId, ct);
                await _notifier.NotifyRankingUpdatedAsync(sessionId, rankingAfterPenalty, ct);
            }

            _logger.LogInformation(
                "Session {SessionId} auto-released clue {ClueId} for stage '{Stage}'",
                sessionId, clue.Id, currentStage.StageName);
        }
    }

    private static int MissionDurationSeconds(int declaredTimeMinutes)
        => declaredTimeMinutes == -1 ? UnsetTimeMinutesDefaultSeconds : Math.Max(0, declaredTimeMinutes) * 60;

    public async Task EnforceSessionTimeoutAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _repository.GetByIdWithStagesAsync(sessionId, ct);
        if (session is null || session.Status != SessionStatus.Active || !session.CurrentMissionStartedAt.HasValue)
            return;

        var currentStage = session.GetCurrentStage();
        if (currentStage is null) return;

        var missionStages = session.Stages.Where(s => s.MissionId == currentStage.MissionId).ToList();
        var durationSeconds = MissionDurationSeconds(missionStages[0].TimeMinutes);
        if (durationSeconds <= 0) return; // no enforceable limit configured

        var elapsedSeconds = (DateTime.UtcNow - session.CurrentMissionStartedAt.Value).TotalSeconds;
        if (elapsedSeconds < durationSeconds) return;

        var lastStageOrderOfMission = missionStages.Max(s => s.Order);

        if (lastStageOrderOfMission >= session.Stages.Count)
        {
            // Timed-out mission was the last one in the session — end it.
            await _repository.AddAuditEventAsync(SessionAuditEvent.Create(
                session.Id,
                SessionAuditEventTypes.StatusChanged,
                $"Sesión finalizada automáticamente: tiempo máximo agotado para la misión '{currentStage.MissionTitle}'"), ct);

            await _facade.TransitionAndNotify(session.Id, nameof(SessionStatus.Finished), ct);

            _logger.LogInformation(
                "Session {SessionId} auto-finished: mission '{Mission}' timed out", session.Id, currentStage.MissionTitle);
            return;
        }

        var previousOrder = session.CurrentStageOrder;
        while (session.CurrentStageOrder < lastStageOrderOfMission)
        {
            session.AdvanceStage();
        }
        await _repository.UpdateAsync(session, ct);

        foreach (var participant in session.Participants.Where(p => p.CurrentStageOrder <= previousOrder))
        {
            participant.CatchUpTo(session.CurrentStageOrder);
            await _repository.UpdateParticipantAsync(participant, ct);
        }

        await _repository.AddAuditEventAsync(SessionAuditEvent.Create(
            session.Id,
            SessionAuditEventTypes.StatusChanged,
            $"Misión '{currentStage.MissionTitle}' avanzada automáticamente: tiempo máximo agotado"), ct);

        await _facade.NotifyStageAdvanced(session.Id, session.CurrentStageOrder, ct);

        _logger.LogInformation(
            "Session {SessionId} auto-advanced from stage {Previous} to {Current}: mission '{Mission}' timed out",
            session.Id, previousOrder, session.CurrentStageOrder, currentStage.MissionTitle);
    }
}
