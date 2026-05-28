using FluentAssertions;
using FluentValidation.TestHelper;
using Missions.Application.Missions.Stages;
using Xunit;

namespace Missions.Application.Tests.Missions.Stages;

public class CreateStageCommandValidatorTests
{
    private readonly CreateStageCommandValidator _sut = new();

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new CreateStageCommand(Guid.NewGuid(), "Stage Name", "Stage Description", 1);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyName_ShouldFail()
    {
        // Arrange
        var command = new CreateStageCommand(Guid.NewGuid(), "", "Stage Description", 1);

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
        var longName = new string('a', 201);
        var command = new CreateStageCommand(Guid.NewGuid(), longName, "Stage Description", 1);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name must not exceed 200 characters");
    }

    [Fact]
    public void Validate_DescriptionExceeds2000Characters_ShouldFail()
    {
        // Arrange
        var longDescription = new string('a', 2001);
        var command = new CreateStageCommand(Guid.NewGuid(), "Stage Name", longDescription, 1);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description must not exceed 2000 characters");
    }

    [Fact]
    public void Validate_OrderZero_ShouldFail()
    {
        // Arrange
        var command = new CreateStageCommand(Guid.NewGuid(), "Stage Name", "Stage Description", 0);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Order)
            .WithErrorMessage("Order must be greater than 0");
    }

    [Fact]
    public void Validate_OrderNegative_ShouldFail()
    {
        // Arrange
        var command = new CreateStageCommand(Guid.NewGuid(), "Stage Name", "Stage Description", -1);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Order)
            .WithErrorMessage("Order must be greater than 0");
    }
}
