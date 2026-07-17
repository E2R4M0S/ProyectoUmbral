using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sessions.Api.Endpoints;
using Sessions.Application.Common.Interfaces;
using Sessions.Domain.Entities;
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

    // Mirrors the /internal/participants/score lambda.
    private async Task SimulateParticipantScore(ParticipantScoreRequest req)
    {
        if (req.Delta <= 0) return;
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
        await _repository.AddTeamScoreAsync(req.TeamId, req.Delta);
        var team = await _repository.GetTeamByIdAsync(req.TeamId, CancellationToken.None);
        if (team is not null)
        {
            await _repository.AddAuditEventAsync(SessionAuditEvent.Create(
                team.SessionId,
                SessionAuditEventTypes.TriviaAnswerScored,
                "Puntaje otorgado al equipo por respuesta de trivia",
                teamId: req.TeamId,
                scoreDelta: req.Delta));
        }
    }

    [Fact]
    public async Task ParticipantScore_WithPositiveDelta_ShouldUpdateScoreAndRecordAuditEvent()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();

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
    public async Task TeamScore_WithPositiveDelta_ShouldUpdateScoreAndRecordAuditEventScopedToTeamSession()
    {
        var teamId = Guid.NewGuid();
        var team = SessionTeam.Create(Guid.NewGuid(), "Team A");
        typeof(SessionTeam).GetProperty(nameof(SessionTeam.Id))!.SetValue(team, teamId);
        _repository.GetTeamByIdAsync(teamId, Arg.Any<CancellationToken>()).Returns(team);

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
    public async Task TeamScore_WhenTeamNotFound_ShouldSkipAuditEvent()
    {
        var teamId = Guid.NewGuid();
        _repository.GetTeamByIdAsync(teamId, Arg.Any<CancellationToken>()).Returns((SessionTeam?)null);

        await SimulateTeamScore(new TeamScoreRequest(teamId, 25));

        await _repository.Received(1).AddTeamScoreAsync(teamId, 25, Arg.Any<CancellationToken>());
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
