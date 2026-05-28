using FluentAssertions;
using FluentValidation.TestHelper;
using Missions.Application.Missions.StatusChange;
using Xunit;

namespace Missions.Application.Tests.Missions.StatusChange;

public class ChangeMissionStatusCommandValidatorTests
{
    private readonly ChangeMissionStatusCommandValidator _sut = new();

    [Theory]
    [InlineData("Active")]
    [InlineData("Inactive")]
    public void Validate_ValidStatus_ShouldPass(string status)
    {
        // Arrange
        var command = new ChangeMissionStatusCommand(Guid.NewGuid(), status);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Deleted", "Status must be 'Active' or 'Inactive'")]
    [InlineData("", "Status is required")]
    [InlineData("Draft", "Status must be 'Active' or 'Inactive'")]
    public void Validate_InvalidStatus_ShouldFail(string status, string expectedError)
    {
        // Arrange
        var command = new ChangeMissionStatusCommand(Guid.NewGuid(), status);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains(expectedError.Split(' ')[0]));
    }

    [Fact]
    public void Validate_EmptyId_ShouldFail()
    {
        // Arrange
        var command = new ChangeMissionStatusCommand(Guid.Empty, "Active");

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }
}