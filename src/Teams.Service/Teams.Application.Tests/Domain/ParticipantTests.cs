using FluentAssertions;
using Teams.Domain.Entities;
using Teams.Domain.ValueObjects;
using Xunit;

namespace Teams.Application.Tests.Domain;

public class ParticipantTests
{
    [Fact]
    public void Update_ShouldAssignNameAndAlias_WhenValidInput()
    {
        // Arrange
        var participant = Participant.Create("John Doe", "johnny", "john@test.com", "kc-123");

        // Act
        participant.Update("Jane Doe", "jane_alias");

        // Assert
        participant.Name.Should().Be("Jane Doe");
        participant.Alias.Should().Be("jane_alias");
    }

    [Fact]
    public void Update_ShouldTrimName()
    {
        // Arrange
        var participant = Participant.Create("John Doe", "johnny", "john@test.com", "kc-123");

        // Act
        participant.Update("  Jane Doe  ", "jane_alias");

        // Assert
        participant.Name.Should().Be("Jane Doe");
    }

    [Fact]
    public void Update_ShouldTrimAlias()
    {
        // Arrange
        var participant = Participant.Create("John Doe", "johnny", "john@test.com", "kc-123");

        // Act
        participant.Update("Jane Doe", "  jane_alias  ");

        // Assert
        participant.Alias.Should().Be("jane_alias");
    }

    [Fact]
    public void Update_ShouldThrow_WhenAliasIsInvalid()
    {
        // Arrange
        var participant = Participant.Create("John Doe", "johnny", "john@test.com", "kc-123");

        // Act
        Action act = () => participant.Update("Jane Doe", "ab"); // too short

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Alias*");
    }

    [Fact]
    public void Update_ShouldPreserveReadonlyProperties()
    {
        // Arrange
        var participant = Participant.Create("John Doe", "johnny", "john@test.com", "kc-123");
        var originalId = participant.Id;
        var originalEmail = participant.Email;
        var originalKeycloakUserId = participant.KeycloakUserId;

        // Act
        participant.Update("Jane Doe", "jane_alias");

        // Assert
        participant.Id.Should().Be(originalId);
        participant.Email.Should().Be(originalEmail);
        participant.KeycloakUserId.Should().Be(originalKeycloakUserId);
    }
}