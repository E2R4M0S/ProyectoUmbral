using FluentAssertions;
using FluentValidation.TestHelper;
using Teams.Application.Teams.Register;
using Xunit;

namespace Teams.Application.Tests.Register;

public class RegisterParticipantCommandValidatorTests
{
    private readonly RegisterParticipantCommandValidator _sut = new();

    [Theory]
    [InlineData("John", "Doe", "johndoe", "johnny", "john@test.com", "Pass1234", true)]
    [InlineData("", "Doe", "johndoe", "johnny", "john@test.com", "Pass1234", false)]
    [InlineData("John", "Doe", "johndoe", "", "john@test.com", "Pass1234", false)]
    [InlineData("John", "Doe", "johndoe", "jo", "john@test.com", "Pass1234", false)]
    [InlineData("John", "Doe", "johndoe", "johnny", "", "Pass1234", false)]
    [InlineData("John", "Doe", "johndoe", "johnny", "not-an-email", "Pass1234", false)]
    [InlineData("John", "Doe", "johndoe", "johnny", "john@test.com", "short", false)]
    [InlineData("John", "Doe", "johndoe", "johnny", "john@test.com", "", false)]
    [InlineData("  ", "Doe", "johndoe", "johnny", "john@test.com", "Pass1234", false)] // whitespace-only firstName
    [InlineData("John", "", "johndoe", "johnny", "john@test.com", "Pass1234", false)]
    public void Validate_ShouldReturnExpectedResult(
        string firstName,
        string lastName,
        string username,
        string alias,
        string email,
        string password,
        bool expectedValid)
    {
        // Arrange
        var command = new RegisterParticipantCommand(firstName, lastName, username, alias, email, password);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().Be(expectedValid);
    }
}
