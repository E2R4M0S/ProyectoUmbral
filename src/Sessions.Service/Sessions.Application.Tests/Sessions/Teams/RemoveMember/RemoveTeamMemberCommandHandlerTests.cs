using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.Teams.RemoveMember;
using Sessions.Domain.Entities;
using Xunit;

namespace Sessions.Application.Tests.Sessions.Teams.RemoveMember;

public class RemoveTeamMemberCommandHandlerTests
{
    private readonly ISessionRepository _repository = Substitute.For<ISessionRepository>();
    private readonly ILogger<RemoveTeamMemberCommandHandler> _logger =
        Substitute.For<ILogger<RemoveTeamMemberCommandHandler>>();
    private readonly RemoveTeamMemberCommandHandler _sut;

    public RemoveTeamMemberCommandHandlerTests()
    {
        _sut = new RemoveTeamMemberCommandHandler(_repository, _logger);
    }

    [Fact]
    public async Task Handle_WhenTeamNotFound_ShouldThrow()
    {
        var command = new RemoveTeamMemberCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        _repository.GetTeamByIdAsync(command.TeamId, Arg.Any<CancellationToken>()).Returns((SessionTeam?)null);

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no encontrado*");
    }

    [Fact]
    public async Task Handle_WhenTeamBelongsToDifferentSession_ShouldThrow()
    {
        var team = SessionTeam.Create(Guid.NewGuid(), "Los Ganadores");
        var command = new RemoveTeamMemberCommand(Guid.NewGuid(), team.Id, Guid.NewGuid());
        _repository.GetTeamByIdAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no pertenece a esta sesión*");
    }

    [Fact]
    public async Task Handle_WhenUserNotAMember_ShouldThrow()
    {
        var sessionId = Guid.NewGuid();
        var team = SessionTeam.Create(sessionId, "Los Ganadores");
        var command = new RemoveTeamMemberCommand(sessionId, team.Id, Guid.NewGuid());
        _repository.GetTeamByIdAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no es miembro*");
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldRemoveMemberAndPersist()
    {
        var sessionId = Guid.NewGuid();
        var team = SessionTeam.Create(sessionId, "Los Ganadores");
        var userId = Guid.NewGuid();
        team.AddMember(userId, "jugador1");
        var command = new RemoveTeamMemberCommand(sessionId, team.Id, userId);
        _repository.GetTeamByIdAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);

        await _sut.Handle(command, CancellationToken.None);

        team.Members.Should().BeEmpty();
        await _repository.Received(1).UpdateTeamAsync(team, Arg.Any<CancellationToken>());
    }
}
