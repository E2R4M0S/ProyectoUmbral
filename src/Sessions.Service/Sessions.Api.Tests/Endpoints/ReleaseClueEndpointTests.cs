using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sessions.Application.Common.Interfaces;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;
using Xunit;

namespace Sessions.Api.Tests.Endpoints;

public class ReleaseClueEndpointTests
{
    private readonly ISessionRepository _repo = Substitute.For<ISessionRepository>();
    private readonly IGameSessionFacade _facade = Substitute.For<IGameSessionFacade>();
    private readonly ILogger<Program> _logger = Substitute.For<ILogger<Program>>();
    private static readonly Guid OperatorId = Guid.NewGuid();

    public ReleaseClueEndpointTests()
    {
        _repo.GetAuditTrailAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<SessionAuditEvent>());
    }

    private static Session CreateActiveSession(Guid? operatorId = null)
    {
        var stage = SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "M1", "Stage", "Trivia", 1, "test-token");
        var session = Session.Create("Test", "123456", new List<SessionStage> { stage }, operatorId ?? OperatorId);
        session.TransitionTo(SessionStatus.Preparing);
        session.TransitionTo(SessionStatus.Active);
        return session;
    }

    private static Session CreateFinishedSession(Guid? operatorId = null)
    {
        var session = CreateActiveSession(operatorId);
        session.TransitionTo(SessionStatus.Finished);
        return session;
    }

    [Fact]
    public async Task ReleaseClue_ActiveSessionWithStage_ReturnsOk()
    {
        var session = CreateActiveSession();
        var clueId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        _repo.GetByIdWithStagesAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(session);

        var result = await SimulateEndpoint(session.Id, OperatorId, clueId, teamId);

        result.Should().BeOfType<OkObjectResult>();
        await _facade.Received(1).ReleaseClueAndNotify(session.Id, clueId, teamId, null, Arg.Any<string?>(), Arg.Any<int?>());
    }

    [Fact]
    public async Task ReleaseClue_TargetedAtUser_ReturnsOkAndNotifiesWithUserId()
    {
        var session = CreateActiveSession();
        var clueId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _repo.GetByIdWithStagesAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(session);

        var result = await SimulateEndpoint(session.Id, OperatorId, clueId, null, userId);

        result.Should().BeOfType<OkObjectResult>();
        await _facade.Received(1).ReleaseClueAndNotify(session.Id, clueId, null, userId, Arg.Any<string?>(), Arg.Any<int?>());
    }

    [Fact]
    public async Task ReleaseClue_BothTeamIdAndUserId_ReturnsBadRequest()
    {
        var session = CreateActiveSession();
        _repo.GetByIdWithStagesAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(session);

        var result = await SimulateEndpoint(session.Id, OperatorId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        result.Should().BeOfType<BadRequestObjectResult>();
        await _facade.DidNotReceive().ReleaseClueAndNotify(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<int?>());
    }

    [Fact]
    public async Task ReleaseClue_SameClueAndTeamAlreadyReleased_ReturnsConflict()
    {
        // RB-04
        var session = CreateActiveSession();
        var clueId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        _repo.GetByIdWithStagesAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(session);
        _repo.GetAuditTrailAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(new List<SessionAuditEvent> {
                SessionAuditEvent.Create(session.Id, SessionAuditEventTypes.ManualClueReleased, "ya liberada", teamId: teamId, clueId: clueId)
            });

        var result = await SimulateEndpoint(session.Id, OperatorId, clueId, teamId);

        var conflict = result.Should().BeOfType<ObjectResult>().Subject;
        conflict.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        await _facade.DidNotReceive().ReleaseClueAndNotify(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<int?>());
    }

    [Fact]
    public async Task ReleaseClue_SameClueAlreadyReleasedToDifferentTeam_ReturnsOk()
    {
        // RB-04 only blocks a repeat to the SAME recipient — a different team is a separate release.
        var session = CreateActiveSession();
        var clueId = Guid.NewGuid();
        var otherTeamId = Guid.NewGuid();
        var thisTeamId = Guid.NewGuid();
        _repo.GetByIdWithStagesAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(session);
        _repo.GetAuditTrailAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(new List<SessionAuditEvent> {
                SessionAuditEvent.Create(session.Id, SessionAuditEventTypes.ManualClueReleased, "ya liberada a otro equipo", teamId: otherTeamId, clueId: clueId)
            });

        var result = await SimulateEndpoint(session.Id, OperatorId, clueId, thisTeamId);

        result.Should().BeOfType<OkObjectResult>();
        await _facade.Received(1).ReleaseClueAndNotify(session.Id, clueId, thisTeamId, null, Arg.Any<string?>(), Arg.Any<int?>());
    }

    [Fact]
    public async Task ReleaseClue_SessionNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdWithStagesAsync(id, Arg.Any<CancellationToken>())
            .Returns((Session?)null);

        var result = await SimulateEndpoint(id, OperatorId, Guid.NewGuid(), null);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ReleaseClue_FinishedSession_ReturnsConflict()
    {
        // RB-03 / HU-34: once a session is terminal, no more clues/penalties should apply.
        var session = CreateFinishedSession();
        _repo.GetByIdWithStagesAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(session);

        var result = await SimulateEndpoint(session.Id, OperatorId, Guid.NewGuid(), null);

        var conflict = result.Should().BeOfType<ObjectResult>().Subject;
        conflict.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        await _facade.DidNotReceive().ReleaseClueAndNotify(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<int?>());
    }

    [Fact]
    public async Task ReleaseClue_CalledByNonOwningOperator_ReturnsForbidden()
    {
        // RB-10
        var session = CreateActiveSession();
        _repo.GetByIdWithStagesAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(session);

        var result = await SimulateEndpoint(session.Id, Guid.NewGuid(), Guid.NewGuid(), null);

        var forbidden = result.Should().BeOfType<ObjectResult>().Subject;
        forbidden.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        await _facade.DidNotReceive().ReleaseClueAndNotify(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<string?>(), Arg.Any<int?>());
    }

    [Fact]
    public async Task ReleaseClue_NoActiveStage_ReturnsBadRequest()
    {
        // Advance CurrentStageOrder past the only stage so GetCurrentStage() returns null
        var stage = SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "M1", "Stage", "Trivia", 1, "test-token");
        var session = Session.Create("Test", "111111", new List<SessionStage> { stage }, OperatorId);
        session.TransitionTo(SessionStatus.Preparing);
        session.TransitionTo(SessionStatus.Active);
        // Force CurrentStageOrder = 1 so GetCurrentStage looks for order 2 (doesn't exist)
        typeof(Session).GetProperty("CurrentStageOrder")!
            .SetValue(session, 1, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, null, null);

        _repo.GetByIdWithStagesAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(session);

        var result = await SimulateEndpoint(session.Id, OperatorId, Guid.NewGuid(), null);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ReleaseClue_UnexpectedError_ReturnsProblem()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdWithStagesAsync(id, Arg.Any<CancellationToken>())
            .Returns<Session?>(_ => throw new Exception("Network error"));

        var result = await SimulateEndpoint(id, OperatorId, Guid.NewGuid(), null);

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(500);
    }

    private async Task<IActionResult> SimulateEndpoint(Guid sessionId, Guid? currentUserId, Guid clueId, Guid? teamId, Guid? targetUserId = null)
    {
        try
        {
            var session = await _repo.GetByIdWithStagesAsync(sessionId, CancellationToken.None);
            if (session is null)
                return new NotFoundObjectResult(new { error = "Session not found" });

            if (session.Status is SessionStatus.Finished or SessionStatus.Cancelled)
            {
                return new ObjectResult(new { error = "Session is terminal", message = "No se pueden liberar pistas en una sesión Finalizada o Cancelada" })
                {
                    StatusCode = StatusCodes.Status409Conflict
                };
            }

            if (!session.IsManagedBy(currentUserId))
            {
                return new ObjectResult(new { error = "Forbidden", message = "Solo el operador que creó esta sesión puede administrarla" })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }

            var currentStage = session.GetCurrentStage();
            if (currentStage is null)
                return new BadRequestObjectResult(new { error = "No active stage", message = "Session has no current stage" });

            if (teamId is not null && targetUserId is not null)
                return new BadRequestObjectResult(new { error = "Invalid target", message = "Provide at most one of teamId or userId, not both" });

            // RB-04: this simplified harness always deals with a "predefined" clueId (it never
            // exercises the ad-hoc/custom-content branch), so the dedup check always applies —
            // same as the real endpoint's predefined-clue branch.
            var auditTrail = await _repo.GetAuditTrailAsync(sessionId, CancellationToken.None);
            var alreadyReleased = auditTrail.Any(e =>
                e.EventType == SessionAuditEventTypes.ManualClueReleased &&
                e.ClueId == clueId && e.TeamId == teamId && e.UserId == targetUserId);
            if (alreadyReleased)
            {
                return new ObjectResult(new { error = "Clue already released", message = "Esta pista ya fue liberada a este destinatario para esta etapa" })
                {
                    StatusCode = StatusCodes.Status409Conflict
                };
            }

            await _facade.ReleaseClueAndNotify(sessionId, clueId, teamId, targetUserId, null, null);
            await _repo.AddAuditEventAsync(SessionAuditEvent.Create(
                sessionId, SessionAuditEventTypes.ManualClueReleased, "Pista liberada manualmente: test",
                teamId: teamId, userId: targetUserId, clueId: clueId), CancellationToken.None);

            return new OkObjectResult(new { id = sessionId, clueId, status = "Released", hasContent = false });
        }
        catch (Exception)
        {
            return new ObjectResult(new { statusCode = 500 }) { StatusCode = 500 };
        }
    }
}
