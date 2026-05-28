using FluentAssertions;
using Teams.Domain.Entities;
using Xunit;

namespace Teams.Application.Tests.Teams.Create;

public class TeamEntityTests
{
    [Fact]
    public void Create_WithValidInputs_ShouldCreateTeam()
    {
        // Arrange
        var name = "Los Leones";
        var description = "Equipo de desarrollo";
        var leaderId = "leader-123";

        // Act
        var team = Team.Create(name, description, leaderId);

        // Assert
        team.Name.Should().Be(name);
        team.Description.Should().Be(description);
        team.LeaderId.Should().Be(leaderId);
        team.Id.Should().NotBeEmpty();
        team.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_ShouldInitializeEmptyMembersList()
    {
        // Arrange
        var name = "Los Tigres";
        var description = "Equipo QA";
        var leaderId = "leader-456";

        // Act
        var team = Team.Create(name, description, leaderId);

        // Assert
        team.Members.Should().NotBeNull();
        team.Members.Should().BeEmpty();
    }

    [Fact]
    public void AddMember_ShouldAddMemberToTeam()
    {
        // Arrange
        var team = Team.Create("Los Halcones", "Equipo frontend", "leader-789");
        var userId = "user-member-1";

        // Act
        team.AddMember(userId);

        // Assert
        team.Members.Should().HaveCount(1);
        team.Members[0].UserId.Should().Be(userId);
    }

    [Fact]
    public void AddMember_ShouldSetCorrectTeamId()
    {
        // Arrange
        var team = Team.Create("Los Búhos", "Equipo backend", "leader-111");
        var userId = "user-member-2";

        // Act
        team.AddMember(userId);

        // Assert
        team.Members[0].TeamId.Should().Be(team.Id);
    }

    [Fact]
    public void AddMember_ShouldGenerateUniqueMemberIds()
    {
        // Arrange
        var team = Team.Create("Los Águilas", "Equipo fullstack", "leader-222");
        var userId1 = "user-1";
        var userId2 = "user-2";

        // Act
        team.AddMember(userId1);
        team.AddMember(userId2);

        // Assert
        team.Members[0].Id.Should().NotBe(team.Members[1].Id);
    }

    [Fact]
    public void Name_ShouldBeTrimmed()
    {
        // Arrange
        var name = "  Los Lobos  ";
        var description = "Equipo móvil";
        var leaderId = "leader-333";

        // Act
        var team = Team.Create(name, description, leaderId);

        // Assert
        team.Name.Should().Be("Los Lobos");
    }
}
