using FluentAssertions;
using Teams.Domain.Entities;
using Xunit;

namespace Teams.Domain.Tests.Entities;

public class TeamMemberTests
{
    [Fact]
    public void Create_WithValidInputs_ShouldCreateTeamMember()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = "user-123";

        // Act
        var member = TeamMember.Create(teamId, userId);

        // Assert
        member.TeamId.Should().Be(teamId);
        member.UserId.Should().Be(userId);
        member.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_ShouldGenerateUniqueIds()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = "user-456";

        // Act
        var member1 = TeamMember.Create(teamId, userId);
        var member2 = TeamMember.Create(teamId, userId);

        // Assert
        member1.Id.Should().NotBe(member2.Id);
    }

    [Fact]
    public void Create_WithNullUserId_ShouldAssignNull()
    {
        // Arrange
        var teamId = Guid.NewGuid();

        // Act
        var member = TeamMember.Create(teamId, null!);

        // Assert
        member.UserId.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldAssignEmpty()
    {
        // Arrange
        var teamId = Guid.NewGuid();

        // Act
        var member = TeamMember.Create(teamId, string.Empty);

        // Assert
        member.UserId.Should().BeEmpty();
    }
}
