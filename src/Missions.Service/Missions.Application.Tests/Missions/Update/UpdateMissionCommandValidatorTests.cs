using FluentAssertions;
using FluentValidation.TestHelper;
using Missions.Application.Missions.Update;
using Xunit;

namespace Missions.Application.Tests.Missions.Update;

public class UpdateMissionCommandValidatorTests
{
    private readonly UpdateMissionCommandValidator _sut = new();

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new UpdateMissionCommand(
            Guid.NewGuid(),
            "Find the Treasure",
            "Hidden treasure in the forest",
            "Easy",
            30);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", "Title", "Description", "Easy", 30, "Id is required")]
    [InlineData("c247af55-6a78-4ee8-b7c7-7cf2dbefc88f", "", "Description", "Easy", 30, "Title is required")]
    [InlineData("c247af55-6a78-4ee8-b7c7-7cf2dbefc88f", "Title", "", "Easy", 30, "Description is required")]
    [InlineData("c247af55-6a78-4ee8-b7c7-7cf2dbefc88f", "Title", "Description", "Invalid", 30, "Difficulty must be Easy, Medium, or Hard")]
    [InlineData("c247af55-6a78-4ee8-b7c7-7cf2dbefc88f", "Title", "Description", "Easy", 45, "TimeMinutes must be one of: 15, 30, 60, 90")]
    public void Validate_InvalidFields_ShouldFail(
        string id,
        string title,
        string description,
        string difficulty,
        int timeMinutes,
        string expectedError)
    {
        // Arrange
        var command = new UpdateMissionCommand(
            Guid.Parse(id),
            title,
            description,
            difficulty,
            timeMinutes);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains(expectedError.Split(' ')[0]));
    }

    [Theory]
    [InlineData("Easy")]
    [InlineData("Medium")]
    [InlineData("Hard")]
    public void Validate_ValidDifficulty_ShouldPass(string difficulty)
    {
        // Arrange
        var command = new UpdateMissionCommand(
            Guid.NewGuid(),
            "Title",
            "Description",
            difficulty,
            30);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(90)]
    public void Validate_ValidTimeMinutes_ShouldPass(int timeMinutes)
    {
        // Arrange
        var command = new UpdateMissionCommand(
            Guid.NewGuid(),
            "Title",
            "Description",
            "Easy",
            timeMinutes);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_TitleExceeds200Characters_ShouldFail()
    {
        // Arrange
        var longTitle = new string('A', 201);
        var command = new UpdateMissionCommand(
            Guid.NewGuid(),
            longTitle,
            "Description",
            "Easy",
            30);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title must not exceed 200 characters");
    }

    [Fact]
    public void Validate_DescriptionExceeds2000Characters_ShouldFail()
    {
        // Arrange
        var longDescription = new string('A', 2001);
        var command = new UpdateMissionCommand(
            Guid.NewGuid(),
            "Title",
            longDescription,
            "Easy",
            30);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description must not exceed 2000 characters");
    }
}
