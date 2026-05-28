using FluentAssertions;
using FluentValidation.TestHelper;
using Missions.Application.Missions.Clues;
using Xunit;

namespace Missions.Application.Tests.Missions.Clues;

public class CreateClueCommandValidatorTests
{
    private readonly CreateClueCommandValidator _sut = new();

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new CreateClueCommand(Guid.NewGuid(), Guid.NewGuid(), "Clue content", null, "Auto");

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyContent_ShouldFail()
    {
        // Arrange
        var command = new CreateClueCommand(Guid.NewGuid(), Guid.NewGuid(), "", null, "Auto");

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("Content is required");
    }

    [Fact]
    public void Validate_ContentExceeds2000Characters_ShouldFail()
    {
        // Arrange
        var longContent = new string('a', 2001);
        var command = new CreateClueCommand(Guid.NewGuid(), Guid.NewGuid(), longContent, null, "Auto");

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Content)
            .WithErrorMessage("Content must not exceed 2000 characters");
    }

    [Fact]
    public void Validate_InvalidReleaseType_ShouldFail()
    {
        // Arrange
        var command = new CreateClueCommand(Guid.NewGuid(), Guid.NewGuid(), "Valid content", null, "Invalid");

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReleaseType)
            .WithErrorMessage("ReleaseType must be 'Auto' or 'Manual'");
    }

    [Fact]
    public void Validate_NegativePenalty_ShouldFail()
    {
        // Arrange
        var command = new CreateClueCommand(Guid.NewGuid(), Guid.NewGuid(), "Valid content", -1, "Auto");

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Penalty)
            .WithErrorMessage("Penalty must be non-negative");
    }
}
