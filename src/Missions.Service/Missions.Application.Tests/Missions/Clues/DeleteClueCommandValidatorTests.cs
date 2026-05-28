using FluentAssertions;
using FluentValidation.TestHelper;
using Missions.Application.Missions.Clues;
using Xunit;

namespace Missions.Application.Tests.Missions.Clues;

public class DeleteClueCommandValidatorTests
{
    private readonly DeleteClueCommandValidator _sut = new();

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new DeleteClueCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyMissionId_ShouldFail()
    {
        // Arrange
        var command = new DeleteClueCommand(Guid.Empty, Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MissionId)
            .WithErrorMessage("MissionId is required");
    }

    [Fact]
    public void Validate_EmptyStageId_ShouldFail()
    {
        // Arrange
        var command = new DeleteClueCommand(Guid.NewGuid(), Guid.Empty, Guid.NewGuid());

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.StageId)
            .WithErrorMessage("StageId is required");
    }

    [Fact]
    public void Validate_EmptyClueId_ShouldFail()
    {
        // Arrange
        var command = new DeleteClueCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ClueId)
            .WithErrorMessage("ClueId is required");
    }
}
