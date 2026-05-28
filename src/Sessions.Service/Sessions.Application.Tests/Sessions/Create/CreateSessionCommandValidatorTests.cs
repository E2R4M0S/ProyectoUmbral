using FluentAssertions;
using FluentValidation.TestHelper;
using Sessions.Application.Sessions.Create;
using Xunit;

namespace Sessions.Application.Tests.Sessions.Create;

public class CreateSessionCommandValidatorTests
{
    private readonly CreateSessionCommandValidator _sut = new();

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new CreateSessionCommand("Test Session", Guid.NewGuid());

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyName_ShouldFail()
    {
        // Arrange
        var command = new CreateSessionCommand("", Guid.NewGuid());

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name is required");
    }

    [Fact]
    public void Validate_NameExceeds200Characters_ShouldFail()
    {
        // Arrange
        var longName = new string('A', 201);
        var command = new CreateSessionCommand(longName, Guid.NewGuid());

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name must not exceed 200 characters");
    }

    [Fact]
    public void Validate_NameWith200Characters_ShouldPass()
    {
        // Arrange
        var name200 = new string('A', 200);
        var command = new CreateSessionCommand(name200, Guid.NewGuid());

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyMissionId_ShouldFail()
    {
        // Arrange
        var command = new CreateSessionCommand("Test Session", Guid.Empty);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MissionId)
            .WithErrorMessage("MissionId cannot be empty");
    }

    [Theory]
    [InlineData("Session A")]
    [InlineData("A")]
    [InlineData("Special Characters: !@#$%")]
    public void Validate_ValidNames_ShouldPass(string name)
    {
        // Arrange
        var command = new CreateSessionCommand(name, Guid.NewGuid());

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
