using FluentAssertions;
using Sessions.Application.Sessions.Transition;
using Xunit;

namespace Sessions.Application.Tests.Sessions.Transition;

public class TransitionSessionCommandValidatorTests
{
    private readonly TransitionSessionCommandValidator _sut;

    public TransitionSessionCommandValidatorTests()
    {
        _sut = new TransitionSessionCommandValidator();
    }

    [Fact]
    public void Validate_WithValidCommand_ShouldNotHaveErrors()
    {
        // Arrange
        var command = new TransitionSessionCommand(Guid.NewGuid(), "Active");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Scheduled")]
    [InlineData("Preparing")]
    [InlineData("Active")]
    [InlineData("Paused")]
    [InlineData("Finished")]
    [InlineData("Cancelled")]
    public void Validate_WithValidStatus_ShouldNotHaveErrors(string status)
    {
        // Arrange
        var command = new TransitionSessionCommand(Guid.NewGuid(), status);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("InvalidStatus")]
    [InlineData("ACTIVE")]
    [InlineData("active")]
    [InlineData("")]
    public void Validate_WithInvalidStatus_ShouldHaveErrors(string status)
    {
        // Arrange
        var command = new TransitionSessionCommand(Guid.NewGuid(), status);

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewStatus");
    }

    [Fact]
    public void Validate_WithEmptyId_ShouldHaveErrors()
    {
        // Arrange
        var command = new TransitionSessionCommand(Guid.Empty, "Active");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    [Fact]
    public void Validate_WithEmptyStatus_ShouldHaveErrors()
    {
        // Arrange
        var command = new TransitionSessionCommand(Guid.NewGuid(), "");

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewStatus");
    }
}