using FluentAssertions;
using Teams.Domain.Entities;
using Xunit;

namespace Teams.Domain.Tests.Entities;

public class ParticipantTests
{
    // ===== Create Tests =====

    [Fact]
    public void Create_Valid_ShouldCreateParticipant()
    {
        var participant = Participant.Create("John", "Doe", "johndoe", "johnny", "john@test.com", "kc-123");

        participant.FirstName.Should().Be("John");
        participant.LastName.Should().Be("Doe");
        participant.Alias.Should().Be("johnny");
        participant.Email.Should().Be("john@test.com");
        participant.KeycloakUserId.Should().Be("kc-123");
        participant.Id.Should().NotBeEmpty();
        participant.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_InvalidAlias_ShouldThrow()
    {
        Action act = () => Participant.Create("John", "Doe", "johndoe", "ab", "john@test.com", "kc-123");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Alias*");
    }

    [Fact]
    public void Create_InvalidEmail_ShouldThrow()
    {
        Action act = () => Participant.Create("John", "Doe", "johndoe", "johnny", "invalid-email", "kc-123");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Email*");
    }

    [Fact]
    public void Create_TrimsFirstName()
    {
        var participant = Participant.Create("  John  ", "Doe", "johndoe", "johnny", "john@test.com", "kc-123");

        participant.FirstName.Should().Be("John");
    }

    [Fact]
    public void Create_TrimsAlias()
    {
        var participant = Participant.Create("John", "Doe", "johndoe", "  johnny  ", "john@test.com", "kc-123");

        participant.Alias.Should().Be("johnny");
    }

    [Fact]
    public void Create_NormalizesEmail()
    {
        var participant = Participant.Create("John", "Doe", "johndoe", "johnny", "JOHN@TEST.COM", "kc-123");

        participant.Email.Should().Be("john@test.com");
    }

    // ===== Update Tests =====

    [Fact]
    public void Update_NameAndAlias_ShouldAssignNewValues()
    {
        var participant = Participant.Create("John", "Doe", "johndoe", "johnny", "john@test.com", "kc-123");

        participant.Update("Jane", "Doe", "jane_alias");

        participant.FirstName.Should().Be("Jane");
        participant.LastName.Should().Be("Doe");
        participant.Alias.Should().Be("jane_alias");
    }

    [Fact]
    public void Update_TrimFirstName()
    {
        var participant = Participant.Create("John", "Doe", "johndoe", "johnny", "john@test.com", "kc-123");

        participant.Update("  Jane  ", "Doe", "jane_alias");

        participant.FirstName.Should().Be("Jane");
    }

    [Fact]
    public void Update_TrimAlias()
    {
        var participant = Participant.Create("John", "Doe", "johndoe", "johnny", "john@test.com", "kc-123");

        participant.Update("Jane", "Doe", "  jane_alias  ");

        participant.Alias.Should().Be("jane_alias");
    }

    [Fact]
    public void Update_InvalidAlias_ShouldThrow()
    {
        var participant = Participant.Create("John", "Doe", "johndoe", "johnny", "john@test.com", "kc-123");

        Action act = () => participant.Update("Jane", "Doe", "ab");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Alias*");
    }

    [Fact]
    public void Update_EmptyFirstName_ShouldAssignEmpty()
    {
        var participant = Participant.Create("John", "Doe", "johndoe", "johnny", "john@test.com", "kc-123");

        participant.Update("", "Doe", "valid_alias");

        participant.FirstName.Should().BeEmpty();
    }

    [Fact]
    public void Update_PreserveReadonlyProperties()
    {
        var participant = Participant.Create("John", "Doe", "johndoe", "johnny", "john@test.com", "kc-123");
        var originalId = participant.Id;
        var originalEmail = participant.Email;
        var originalKeycloakUserId = participant.KeycloakUserId;

        participant.Update("Jane", "Doe", "jane_alias");

        participant.Id.Should().Be(originalId);
        participant.Email.Should().Be(originalEmail);
        participant.KeycloakUserId.Should().Be(originalKeycloakUserId);
    }
}
