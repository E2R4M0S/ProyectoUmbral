using FluentAssertions;
using FluentValidation.TestHelper;
using Teams.Application.Teams.Operators.Disable;
using Xunit;

namespace Teams.Application.Tests.Teams.Operators.Disable;

public class DisableOperatorCommandValidatorTests
{
    private readonly DisableOperatorCommandValidator _sut = new();

    [Theory]
    [InlineData("juan@test.com", true)]
    [InlineData("operador@example.com", true)]
    [InlineData("", false)]
    [InlineData("not-an-email", false)]
    [InlineData("   ", false)]
    public void Validate_ShouldReturnExpectedResult(
        string email,
        bool expectedValid)
    {
        // Arrange
        var command = new DisableOperatorCommand(email);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().Be(expectedValid);
    }

    [Fact]
    public void Validate_EmptyEmail_ShouldHaveErrorForEmail()
    {
        // Arrange
        var command = new DisableOperatorCommand("");

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_InvalidEmailFormat_ShouldHaveErrorForEmail()
    {
        // Arrange
        var command = new DisableOperatorCommand("not-an-email");

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WhitespaceEmail_ShouldHaveErrorForEmail()
    {
        // Arrange
        var command = new DisableOperatorCommand("   ");

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_ValidEmail_ShouldHaveNoErrors()
    {
        // Arrange
        var command = new DisableOperatorCommand("juan@test.com");

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().NotContain(e => e.PropertyName == "Email");
    }
}