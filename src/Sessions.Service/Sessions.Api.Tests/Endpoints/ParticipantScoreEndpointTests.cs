using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sessions.Api.Endpoints;
using Sessions.Application.Common.Interfaces;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;
using Xunit;

namespace Sessions.Api.Tests.Endpoints;

/// <summary>
/// Unit-style tests for ParticipantScoreEndpoint's internal handlers.
/// Mirrors the AdvanceStageEndpointTests pattern: simulate the endpoint lambda inline
/// because we don't have WebApplicationFactory infrastructure yet.
/// </summary>
public class ParticipantScoreEndpointTests
{
    private readonly ISessionRepository _repository = Substitute.For<ISessionRepository>();
    private readonly ILogger<Program> _logger = Substitute.For<ILogger<Program>>();

    private static Session CreateActiveSession(Guid sessionId)
    {
        var stage = SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "M1", "Stage", "Trivia", 1, "test-token");
        var session = Session.Create("Test", "123456", new List<SessionStage> { stage }, Guid.NewGuid());
        typeof(Session).GetProperty(nameof(Session.Id))!.SetValue(session, sessionId);
        session.TransitionTo(SessionStatus.Preparing);
        session.TransitionTo(SessionStatus.Active);
        return session;
    }

    private static Session CreateFinishedSession(Guid sessionId)
    {
        var session = CreateActiveSession(sessionId);
        session.TransitionTo(SessionStatus.Finished);
        return session;
    }

    // Mirrors the /internal/participants/score lambda.
    private async Task SimulateParticipantScore(ParticipantScoreRequest req)
    {
        if (req.Delta <= 0) return;

        var session = await _repository.GetByIdAsync(req.SessionId, CancellationToken.None);
        if (session is null || session.Status is SessionStatus.Finished or SessionStatus.Cancelled) return;

        await _repository.AddParticipantScoreAsync(req.SessionId, req.UserId, req.Delta);
        await _repository.AddAuditEventAsync(SessionAuditEvent.Create(
            req.SessionId,
            SessionAuditEventTypes.TriviaAnswerScored,
            "Puntaje otorgado por respuesta de trivia",
            userId: req.UserId,
            scoreDelta: req.Delta));
    }

    // Mirrors the /internal/teams/score lambda.
    private async Task SimulateTeamScore(TeamScoreRequest req)
    {
        if (req.Delta <= 0) return;

        var team = await _repository.GetTeamByIdAsync(req.TeamId, CancellationToken.None);
        if (team is null) return;

        var session = await _repository.GetByIdAsync(team.SessionId, CancellationToken.None);
        if (session is null || session.Status is SessionStatus.Finished or SessionStatus.Cancelled) return;

        await _repository.AddTeamScoreAsync(req.TeamId, req.Delta);
        await _repository.AddAuditEventAsync(SessionAuditEvent.Create(
            team.SessionId,
            SessionAuditEventTypes.TriviaAnswerScored,
            "Puntaje otorgado al equipo por respuesta de trivia",
            teamId: req.TeamId,
            scoreDelta: req.Delta));
    }

    [Fact]
    public async Task ParticipantScore_WithPositiveDelta_ShouldUpdateScoreAndRecordAuditEvent()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _repository.GetByIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns(CreateActiveSession(sessionId));

        await SimulateParticipantScore(new ParticipantScoreRequest(sessionId, userId, 15));

        await _repository.Received(1).AddParticipantScoreAsync(sessionId, userId, 15, Arg.Any<CancellationToken>());
        await _repository.Received(1).AddAuditEventAsync(
            Arg.Is<SessionAuditEvent>(e =>
                e.SessionId == sessionId &&
                e.UserId == userId &&
                e.EventType == SessionAuditEventTypes.TriviaAnswerScored &&
                e.ScoreDelta == 15),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ParticipantScore_WithNonPositiveDelta_ShouldDoNothing()
    {
        await SimulateParticipantScore(new ParticipantScoreRequest(Guid.NewGuid(), Guid.NewGuid(), 0));

        await _repository.DidNotReceive().AddParticipantScoreAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().AddAuditEventAsync(Arg.Any<SessionAuditEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ParticipantScore_WhenSessionFinished_ShouldSkipScoreAndAuditEvent()
    {
        // RB-03 / HU-34: score must stay frozen once the session is terminal.
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _repository.GetByIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns(CreateFinishedSession(sessionId));

        await SimulateParticipantScore(new ParticipantScoreRequest(sessionId, userId, 15));

        await _repository.DidNotReceive().AddParticipantScoreAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().AddAuditEventAsync(Arg.Any<SessionAuditEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ParticipantScore_WhenSessionNotFound_ShouldSkipScoreAndAuditEvent()
    {
        var sessionId = Guid.NewGuid();
        _repository.GetByIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns((Session?)null);

        await SimulateParticipantScore(new ParticipantScoreRequest(sessionId, Guid.NewGuid(), 15));

        await _repository.DidNotReceive().AddParticipantScoreAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().AddAuditEventAsync(Arg.Any<SessionAuditEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TeamScore_WithPositiveDelta_ShouldUpdateScoreAndRecordAuditEventScopedToTeamSession()
    {
        var teamId = Guid.NewGuid();
        var team = SessionTeam.Create(Guid.NewGuid(), "Team A");
        typeof(SessionTeam).GetProperty(nameof(SessionTeam.Id))!.SetValue(team, teamId);
        _repository.GetTeamByIdAsync(teamId, Arg.Any<CancellationToken>()).Returns(team);
        _repository.GetByIdAsync(team.SessionId, Arg.Any<CancellationToken>()).Returns(CreateActiveSession(team.SessionId));

        await SimulateTeamScore(new TeamScoreRequest(teamId, 25));

        await _repository.Received(1).AddTeamScoreAsync(teamId, 25, Arg.Any<CancellationToken>());
        await _repository.Received(1).AddAuditEventAsync(
            Arg.Is<SessionAuditEvent>(e =>
                e.SessionId == team.SessionId &&
                e.TeamId == teamId &&
                e.EventType == SessionAuditEventTypes.TriviaAnswerScored &&
                e.ScoreDelta == 25),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TeamScore_WhenTeamNotFound_ShouldSkipScoreAndAuditEvent()
    {
        var teamId = Guid.NewGuid();
        _repository.GetTeamByIdAsync(teamId, Arg.Any<CancellationToken>()).Returns((SessionTeam?)null);

        await SimulateTeamScore(new TeamScoreRequest(teamId, 25));

        await _repository.DidNotReceive().AddTeamScoreAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().AddAuditEventAsync(Arg.Any<SessionAuditEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TeamScore_WhenSessionFinished_ShouldSkipScoreAndAuditEvent()
    {
        // RB-03 / HU-34: score must stay frozen once the session is terminal.
        var teamId = Guid.NewGuid();
        var team = SessionTeam.Create(Guid.NewGuid(), "Team A");
        typeof(SessionTeam).GetProperty(nameof(SessionTeam.Id))!.SetValue(team, teamId);
        _repository.GetTeamByIdAsync(teamId, Arg.Any<CancellationToken>()).Returns(team);
        _repository.GetByIdAsync(team.SessionId, Arg.Any<CancellationToken>()).Returns(CreateFinishedSession(team.SessionId));

        await SimulateTeamScore(new TeamScoreRequest(teamId, 25));

        await _repository.DidNotReceive().AddTeamScoreAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().AddAuditEventAsync(Arg.Any<SessionAuditEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TeamScore_WithNonPositiveDelta_ShouldDoNothing()
    {
        await SimulateTeamScore(new TeamScoreRequest(Guid.NewGuid(), 0));

        await _repository.DidNotReceive().AddTeamScoreAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().GetTeamByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
