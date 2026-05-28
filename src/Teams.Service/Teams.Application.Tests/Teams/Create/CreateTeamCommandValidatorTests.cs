using FluentAssertions;
using FluentValidation.TestHelper;
using Teams.Application.Teams.Create;
using Xunit;

namespace Teams.Application.Tests.Teams.Create;

public class CreateTeamCommandValidatorTests
{
    private readonly CreateTeamCommandValidator _sut = new();

    [Fact]
    public void Validate_ValidCommand_ShouldHaveNoErrors()
    {
        // Arrange
        var command = new CreateTeamCommand(
            Name: "Los Leones",
            Description: "Equipo de desarrollo",
            LeaderId: "leader-123",
            MemberIds: new List<string> { "user-1", "user-2" });

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Name is required")]
    [InlineData(null, "Name is required")]
    public void Validate_NameRequired_ShouldHaveError(string? name, string expectedError)
    {
        // Arrange
        var command = new CreateTeamCommand(name!, "Description", "leader-1", new List<string>());

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NameExceeds100Chars_ShouldHaveError()
    {
        // Arrange
        var longName = new string('A', 101);
        var command = new CreateTeamCommand(longName, "Description", "leader-1", new List<string>());

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NameAtMaxLength_ShouldBeValid()
    {
        // Arrange
        var maxName = new string('A', 100);
        var command = new CreateTeamCommand(maxName, "Description", "leader-1", new List<string>());

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_DescriptionExceeds500Chars_ShouldHaveError()
    {
        // Arrange
        var longDescription = new string('D', 501);
        var command = new CreateTeamCommand("Valid Name", longDescription, "leader-1", new List<string>());

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_DescriptionAtMaxLength_ShouldBeValid()
    {
        // Arrange
        var maxDescription = new string('D', 500);
        var command = new CreateTeamCommand("Valid Name", maxDescription, "leader-1", new List<string>());

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "LeaderId is required")]
    [InlineData(null, "LeaderId is required")]
    public void Validate_LeaderIdRequired_ShouldHaveError(string? leaderId, string expectedError)
    {
        // Arrange
        var command = new CreateTeamCommand("Name", "Description", leaderId!, new List<string>());

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LeaderId);
    }

    [Fact]
    public void Validate_LeaderIdEmptyString_ShouldHaveError()
    {
        // Arrange
        var command = new CreateTeamCommand("Name", "Description", "", new List<string>());

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LeaderId);
    }

    [Fact]
    public void Validate_WhitespaceLeaderId_ShouldHaveError()
    {
        // Arrange
        var command = new CreateTeamCommand("Name", "Description", "   ", new List<string>());

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LeaderId);
    }
}
