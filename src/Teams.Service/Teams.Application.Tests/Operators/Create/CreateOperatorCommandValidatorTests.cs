using FluentAssertions;
using FluentValidation.TestHelper;
using Teams.Application.Teams.Operators.Create;
using Xunit;

namespace Teams.Application.Tests.Operators.Create;

public class CreateOperatorCommandValidatorTests
{
    private readonly CreateOperatorCommandValidator _sut = new();

    [Theory]
    [InlineData("Juan Pérez", "juan@test.com", true)]
    [InlineData("", "juan@test.com", false)]
    [InlineData("Juan Pérez", "", false)]
    [InlineData("Juan Pérez", "not-an-email", false)]
    [InlineData("  ", "juan@test.com", false)] // whitespace-only name
    public void Validate_ShouldReturnExpectedResult(
        string name,
        string email,
        bool expectedValid)
    {
        // Arrange
        var command = new CreateOperatorCommand(name, email);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().Be(expectedValid);
    }

    [Fact]
    public void Validate_NameExceeds100Characters_ShouldFail()
    {
        // Arrange
        var longName = new string('A', 101);
        var command = new CreateOperatorCommand(longName, "juan@test.com");

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NameExactly100Characters_ShouldPass()
    {
        // Arrange
        var maxName = new string('A', 100);
        var command = new CreateOperatorCommand(maxName, "juan@test.com");

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
