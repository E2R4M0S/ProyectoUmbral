using FluentAssertions;
using Teams.Domain.Entities;
using Xunit;

namespace Teams.Application.Tests.Teams.List;

public class TeamEntityJoinCodeTests
{
    [Fact]
    public void GenerateJoinCode_ShouldReturn6CharAlphanumericCode()
    {
        // Arrange
        var team = Team.Create("Los Leones", "Equipo de desarrollo", "leader-123");

        // Act
        team.GenerateJoinCode();

        // Assert
        team.JoinCode.Should().NotBeNullOrEmpty();
        team.JoinCode.Should().HaveLength(6);
        team.JoinCode.Should().MatchRegex("^[A-Z0-9]{6}$");
    }

    [Fact]
    public void GenerateJoinCode_ShouldGenerateUniqueCodes()
    {
        // Arrange
        var team1 = Team.Create("Los Leones", "Desc 1", "leader-1");
        var team2 = Team.Create("Los Tigres", "Desc 2", "leader-2");
        var team3 = Team.Create("Los Búhos", "Desc 3", "leader-3");

        // Act
        team1.GenerateJoinCode();
        team2.GenerateJoinCode();
        team3.GenerateJoinCode();

        // Assert
        team1.JoinCode.Should().NotBe(team2.JoinCode);
        team1.JoinCode.Should().NotBe(team3.JoinCode);
        team2.JoinCode.Should().NotBe(team3.JoinCode);
    }

    [Fact]
    public void GenerateJoinCode_ShouldBeCalledMultipleTimes_ProducesNewCode()
    {
        // Arrange
        var team = Team.Create("Los Halcones", "Desc", "leader-1");
        team.GenerateJoinCode();
        var firstCode = team.JoinCode;

        // Act
        team.GenerateJoinCode();

        // Assert
        team.JoinCode.Should().NotBe(firstCode);
    }
}
