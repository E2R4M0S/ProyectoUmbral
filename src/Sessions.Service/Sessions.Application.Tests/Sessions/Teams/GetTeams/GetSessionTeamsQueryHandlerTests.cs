using FluentAssertions;
using NSubstitute;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.Teams.GetTeams;
using Sessions.Domain.Entities;
using Xunit;

namespace Sessions.Application.Tests.Sessions.Teams.GetTeams;

public class GetSessionTeamsQueryHandlerTests
{
    private readonly ISessionRepository _repository = Substitute.For<ISessionRepository>();
    private readonly GetSessionTeamsQueryHandler _sut;

    public GetSessionTeamsQueryHandlerTests()
    {
        _sut = new GetSessionTeamsQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenNoTeams_ShouldReturnEmptyList()
    {
        var sessionId = Guid.NewGuid();
        _repository.GetTeamsBySessionIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(new List<SessionTeam>());

        var result = await _sut.Handle(new GetSessionTeamsQuery(sessionId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithTeamsAndMembers_ShouldMapToDtos()
    {
        var sessionId = Guid.NewGuid();
        var team = SessionTeam.Create(sessionId, "Los Ganadores");
        var userId = Guid.NewGuid();
        team.AddMember(userId, "jugador1");

        _repository.GetTeamsBySessionIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(new List<SessionTeam> { team });

        var result = await _sut.Handle(new GetSessionTeamsQuery(sessionId), CancellationToken.None);

        result.Should().ContainSingle();
        var dto = result[0];
        dto.Id.Should().Be(team.Id);
        dto.Name.Should().Be("Los Ganadores");
        dto.MemberCount.Should().Be(1);
        dto.MaxMembers.Should().Be(5);
        dto.Members.Should().ContainSingle(m => m.UserId == userId && m.UserAlias == "jugador1");
    }

    [Fact]
    public async Task Handle_WithMultipleTeams_ShouldReturnAllMapped()
    {
        var sessionId = Guid.NewGuid();
        var teamA = SessionTeam.Create(sessionId, "Equipo A");
        var teamB = SessionTeam.Create(sessionId, "Equipo B");

        _repository.GetTeamsBySessionIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(new List<SessionTeam> { teamA, teamB });

        var result = await _sut.Handle(new GetSessionTeamsQuery(sessionId), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(t => t.Name).Should().Contain(new[] { "Equipo A", "Equipo B" });
    }
}
