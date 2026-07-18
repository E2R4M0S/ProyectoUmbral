using FluentAssertions;
using Missions.Domain.Entities;
using Xunit;

namespace Missions.Domain.Tests.Entities;

public class ParticipantTests
{
    [Fact]
    public void Create_WithValidData_ShouldSetAllProperties()
    {
        var participant = Participant.Create("Juan", "Perez", "juanperez", "jugador1", "juan@example.com", "kc-123");

        participant.Id.Should().NotBeEmpty();
        participant.FirstName.Should().Be("Juan");
        participant.LastName.Should().Be("Perez");
        participant.Username.Should().Be("juanperez");
        participant.Alias.Should().Be("jugador1");
        participant.Email.Should().Be("juan@example.com");
        participant.KeycloakUserId.Should().Be("kc-123");
        participant.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_ShouldTrimNamesAndAlias()
    {
        var participant = Participant.Create("  Juan  ", "  Perez  ", "  juanperez  ", "  jugador1  ", "juan@example.com", "kc-123");

        participant.FirstName.Should().Be("Juan");
        participant.LastName.Should().Be("Perez");
        participant.Username.Should().Be("juanperez");
        participant.Alias.Should().Be("jugador1");
    }

    [Fact]
    public void Create_ShouldLowercaseEmail()
    {
        var participant = Participant.Create("Juan", "Perez", "juanperez", "jugador1", "Juan@Example.COM", "kc-123");

        participant.Email.Should().Be("juan@example.com");
    }

    [Fact]
    public void Create_WithInvalidAlias_ShouldThrow()
    {
        var act = () => Participant.Create("Juan", "Perez", "juanperez", "ab", "juan@example.com", "kc-123");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithInvalidEmail_ShouldThrow()
    {
        var act = () => Participant.Create("Juan", "Perez", "juanperez", "jugador1", "not-an-email", "kc-123");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Update_WithValidData_ShouldUpdateNamesAndAlias()
    {
        var participant = Participant.Create("Juan", "Perez", "juanperez", "jugador1", "juan@example.com", "kc-123");

        participant.Update("Juan Carlos", "Perez Gomez", "jugador2");

        participant.FirstName.Should().Be("Juan Carlos");
        participant.LastName.Should().Be("Perez Gomez");
        participant.Alias.Should().Be("jugador2");
    }

    [Fact]
    public void Update_ShouldNotChangeEmailOrUsername()
    {
        var participant = Participant.Create("Juan", "Perez", "juanperez", "jugador1", "juan@example.com", "kc-123");

        participant.Update("Juan Carlos", "Perez Gomez", "jugador2");

        participant.Email.Should().Be("juan@example.com");
        participant.Username.Should().Be("juanperez");
    }

    [Fact]
    public void Update_WithInvalidAlias_ShouldThrowAndNotChangeState()
    {
        var participant = Participant.Create("Juan", "Perez", "juanperez", "jugador1", "juan@example.com", "kc-123");

        var act = () => participant.Update("Juan Carlos", "Perez Gomez", "x");

        act.Should().Throw<ArgumentException>();
        participant.Alias.Should().Be("jugador1");
    }

    [Fact]
    public void Update_ShouldTrimAlias()
    {
        var participant = Participant.Create("Juan", "Perez", "juanperez", "jugador1", "juan@example.com", "kc-123");

        participant.Update("Juan", "Perez", "  jugador2  ");

        participant.Alias.Should().Be("jugador2");
    }
}
