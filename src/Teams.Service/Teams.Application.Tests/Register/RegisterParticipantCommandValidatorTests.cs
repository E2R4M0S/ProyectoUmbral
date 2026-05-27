using FluentAssertions;
using FluentValidation.TestHelper;
using Teams.Application.Teams.Register;
using Xunit;

namespace Teams.Application.Tests.Register;

public class RegisterParticipantCommandValidatorTests
{
    private readonly RegisterParticipantCommandValidator _sut = new();

    [Theory]
    [InlineData("John Doe", "johnny", "john@test.com", "Pass1234", true)]
    [InlineData("", "johnny", "john@test.com", "Pass1234", false)]
    [InlineData("John Doe", "", "john@test.com", "Pass1234", false)]
    [InlineData("John Doe", "jo", "john@test.com", "Pass1234", false)]
    [InlineData("John Doe", "johnny", "", "Pass1234", false)]
    [InlineData("John Doe", "johnny", "not-an-email", "Pass1234", false)]
    [InlineData("John Doe", "johnny", "john@test.com", "short", false)]
    [InlineData("John Doe", "johnny", "john@test.com", "", false)]
    [InlineData("  ", "johnny", "john@test.com", "Pass1234", false)] // whitespace-only name
    public void Validate_ShouldReturnExpectedResult(
        string name,
        string alias,
        string email,
        string password,
        bool expectedValid)
    {
        // Arrange
        var command = new RegisterParticipantCommand(name, alias, email, password);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().Be(expectedValid);
    }
}
