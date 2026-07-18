using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.Teams.JoinTeam;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;
using Xunit;

namespace Sessions.Application.Tests.Sessions.Teams.JoinTeam;

public class JoinSessionTeamCommandHandlerTests
{
    private readonly ISessionRepository _repository = Substitute.For<ISessionRepository>();
    private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
    private readonly ILogger<JoinSessionTeamCommandHandler> _logger =
        Substitute.For<ILogger<JoinSessionTeamCommandHandler>>();
    private readonly JoinSessionTeamCommandHandler _sut;

    public JoinSessionTeamCommandHandlerTests()
    {
        _sut = new JoinSessionTeamCommandHandler(_repository, _httpContextAccessor, _logger);
    }

    [Fact]
    public async Task Handle_WithoutUserClaim_ShouldThrow()
    {
        _httpContextAccessor.HttpContext = new DefaultHttpContext();
        var command = new JoinSessionTeamCommand(Guid.NewGuid(), Guid.NewGuid());

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*token*");
    }

    [Fact]
    public async Task Handle_WhenSessionNotFound_ShouldThrow()
    {
        var userId = Guid.NewGuid();
        SetupUserId(userId);
        var command = new JoinSessionTeamCommand(Guid.NewGuid(), Guid.NewGuid());
        _repository.GetByIdAsync(command.SessionId, Arg.Any<CancellationToken>()).Returns((Session?)null);

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no encontrada*");
    }

    [Fact]
    public async Task Handle_WhenSessionNotPreparing_ShouldThrow()
    {
        var userId = Guid.NewGuid();
        SetupUserId(userId);
        var session = CreateSessionWithStatus(SessionStatus.Active);
        var command = new JoinSessionTeamCommand(session.Id, Guid.NewGuid());
        _repository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*En Preparación*");
    }

    [Fact]
    public async Task Handle_WhenParticipantNotInSession_ShouldThrow()
    {
        var userId = Guid.NewGuid();
        SetupUserId(userId);
        var session = CreateSessionWithStatus(SessionStatus.Preparing);
        var command = new JoinSessionTeamCommand(session.Id, Guid.NewGuid());
        _repository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _repository.GetParticipantAsync(session.Id, userId, Arg.Any<CancellationToken>())
            .Returns((SessionParticipant?)null);

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*unirte a la sesión*");
    }

    [Fact]
    public async Task Handle_WhenAlreadyMemberOfDifferentTeam_ShouldThrow()
    {
        var userId = Guid.NewGuid();
        SetupUserId(userId);
        var session = CreateSessionWithStatus(SessionStatus.Preparing);
        var command = new JoinSessionTeamCommand(session.Id, Guid.NewGuid());
        var currentTeam = SessionTeam.Create(session.Id, "Equipo Actual");

        _repository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _repository.GetParticipantAsync(session.Id, userId, Arg.Any<CancellationToken>())
            .Returns(SessionParticipant.Create(session.Id, userId));
        _repository.GetParticipantTeamInSessionAsync(session.Id, userId, Arg.Any<CancellationToken>())
            .Returns(currentTeam);

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Ya eres miembro*");
    }

    [Fact]
    public async Task Handle_WhenAlreadyMemberOfSameTeam_ShouldReturnExistingMembershipIdempotently()
    {
        var userId = Guid.NewGuid();
        SetupUserId(userId);
        var session = CreateSessionWithStatus(SessionStatus.Preparing);
        var currentTeam = SessionTeam.Create(session.Id, "Equipo Actual");
        var command = new JoinSessionTeamCommand(session.Id, currentTeam.Id);

        _repository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _repository.GetParticipantAsync(session.Id, userId, Arg.Any<CancellationToken>())
            .Returns(SessionParticipant.Create(session.Id, userId));
        _repository.GetParticipantTeamInSessionAsync(session.Id, userId, Arg.Any<CancellationToken>())
            .Returns(currentTeam);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.TeamId.Should().Be(currentTeam.Id);
        result.UserId.Should().Be(userId);
        await _repository.DidNotReceive().UpdateTeamAsync(Arg.Any<SessionTeam>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTeamNotFound_ShouldThrow()
    {
        var userId = Guid.NewGuid();
        SetupUserId(userId);
        var session = CreateSessionWithStatus(SessionStatus.Preparing);
        var command = new JoinSessionTeamCommand(session.Id, Guid.NewGuid());

        _repository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _repository.GetParticipantAsync(session.Id, userId, Arg.Any<CancellationToken>())
            .Returns(SessionParticipant.Create(session.Id, userId));
        _repository.GetParticipantTeamInSessionAsync(session.Id, userId, Arg.Any<CancellationToken>())
            .Returns((SessionTeam?)null);
        _repository.GetTeamByIdAsync(command.TeamId, Arg.Any<CancellationToken>()).Returns((SessionTeam?)null);

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no encontrado*");
    }

    [Fact]
    public async Task Handle_WhenTeamBelongsToDifferentSession_ShouldThrow()
    {
        var userId = Guid.NewGuid();
        SetupUserId(userId);
        var session = CreateSessionWithStatus(SessionStatus.Preparing);
        var otherSessionTeam = SessionTeam.Create(Guid.NewGuid(), "Otro Equipo");
        var command = new JoinSessionTeamCommand(session.Id, otherSessionTeam.Id);

        _repository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _repository.GetParticipantAsync(session.Id, userId, Arg.Any<CancellationToken>())
            .Returns(SessionParticipant.Create(session.Id, userId));
        _repository.GetParticipantTeamInSessionAsync(session.Id, userId, Arg.Any<CancellationToken>())
            .Returns((SessionTeam?)null);
        _repository.GetTeamByIdAsync(command.TeamId, Arg.Any<CancellationToken>()).Returns(otherSessionTeam);

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no pertenece a esta sesión*");
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldAddMemberAndPersist()
    {
        var userId = Guid.NewGuid();
        SetupUserId(userId, alias: "jugador1");
        var session = CreateSessionWithStatus(SessionStatus.Preparing);
        var team = SessionTeam.Create(session.Id, "Los Ganadores");
        var command = new JoinSessionTeamCommand(session.Id, team.Id);

        _repository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _repository.GetParticipantAsync(session.Id, userId, Arg.Any<CancellationToken>())
            .Returns(SessionParticipant.Create(session.Id, userId));
        _repository.GetParticipantTeamInSessionAsync(session.Id, userId, Arg.Any<CancellationToken>())
            .Returns((SessionTeam?)null);
        _repository.GetTeamByIdAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.TeamId.Should().Be(team.Id);
        result.TeamName.Should().Be("Los Ganadores");
        result.UserId.Should().Be(userId);
        result.UserAlias.Should().Be("jugador1");
        team.Members.Should().ContainSingle(m => m.UserId == userId);
        await _repository.Received(1).UpdateTeamAsync(team, Arg.Any<CancellationToken>());
    }

    private void SetupUserId(Guid userId, string? alias = null)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
        if (alias is not null) claims.Add(new Claim("alias", alias));
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        _httpContextAccessor.HttpContext = httpContext;
    }

    private static Session CreateSessionWithStatus(SessionStatus status)
    {
        var session = Session.Create("Test Session", "123456",
            new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Mission", "Stage", "Trivia", 1, "test-token") });
        var statusProperty = typeof(Session).GetProperty("Status")!;
        statusProperty.SetValue(session, status);
        return session;
    }
}
