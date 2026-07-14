using FluentAssertions;
using FluentValidation.TestHelper;
using Teams.Application.Teams.Profile;
using Xunit;

namespace Teams.Application.Tests.Profile;

public class UpdateProfileCommandValidatorTests
{
    private readonly UpdateProfileCommandValidator _sut = new();

    [Theory]
    [InlineData("John", "Doe", "johnny", true)]
    [InlineData("", "Doe", "johnny", false)]
    [InlineData("John", "Doe", "", false)]
    [InlineData("John", "Doe", "ab", false)] // too short
    [InlineData("John", "Doe", "johnny longer than 50 chars here what do we do", false)] // too long
    [InlineData("John", "Doe", "johnny spaces", false)] // contains space
    [InlineData("John", "Doe", "johnny@special", false)] // contains @
    [InlineData("John", "Doe", "johnny123", true)]
    [InlineData("John", "Doe", "johnny_123", true)]
    [InlineData("John", "Doe", "jo", false)]
    public void Validate_ShouldReturnExpectedResult(string firstName, string lastName, string alias, bool expectedValid)
    {
        // Arrange
        var command = new UpdateProfileCommand(firstName, lastName, alias, "kc-user-123");

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().Be(expectedValid);
    }

    [Fact]
    public void Validate_ShouldPass_WhenAliasIs3Characters()
    {
        // Arrange
        var command = new UpdateProfileCommand("John", "Doe", "abc", "kc-user-123");

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldPass_WhenAliasIs50Characters()
    {
        // Arrange
        var alias50 = new string('a', 50);
        var command = new UpdateProfileCommand("John", "Doe", alias50, "kc-user-123");

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenFirstNameExceeds50Characters()
    {
        // Arrange
        var name51 = new string('a', 51);
        var command = new UpdateProfileCommand(name51, "Doe", "johnny", "kc-user-123");

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }
}
