using FluentAssertions;
using FluentValidation.TestHelper;
using Missions.Application.Missions.Create;
using Xunit;

namespace Missions.Application.Tests.Missions.Create;

public class CreateMissionCommandValidatorTests
{
    private readonly CreateMissionCommandValidator _sut = new();

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new CreateMissionCommand(
            "Find the Treasure",
            "Hidden treasure in the forest",
            "Easy",
            30,
            "Treasure");

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Description", "Easy", 30, "Treasure", false, "Title is required")]
    [InlineData("Title", "", "Easy", 30, "Treasure", false, "Description is required")]
    [InlineData("Title", "Description", "Invalid", 30, "Treasure", false, "Difficulty must be Easy, Medium, or Hard")]
    [InlineData("Title", "Description", "Easy", 45, "Treasure", false, "TimeMinutes must be one of: 15, 30, 60, 90")]
    [InlineData("Title", "Description", "Easy", 30, "Invalid", false, "Type must be Treasure or Trivia")]
    public void Validate_InvalidFields_ShouldFail(
        string title,
        string description,
        string difficulty,
        int timeMinutes,
        string type,
        bool expectedValid,
        string expectedError)
    {
        // Arrange
        var command = new CreateMissionCommand(title, description, difficulty, timeMinutes, type);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().Be(expectedValid);
        if (!expectedValid)
        {
            result.Errors.Should().Contain(e => e.ErrorMessage.Contains(expectedError.Split(' ')[0]));
        }
    }

    [Fact]
    public void Validate_TitleExceeds200Characters_ShouldFail()
    {
        // Arrange
        var longTitle = new string('A', 201);
        var command = new CreateMissionCommand(longTitle, "Description", "Easy", 30, "Treasure");

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
        var command = new CreateMissionCommand("Title", longDescription, "Easy", 30, "Treasure");

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description must not exceed 2000 characters");
    }

    [Theory]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(90)]
    public void Validate_ValidTimeMinutes_ShouldPass(int timeMinutes)
    {
        // Arrange
        var command = new CreateMissionCommand("Title", "Description", "Easy", timeMinutes, "Treasure");

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Easy")]
    [InlineData("Medium")]
    [InlineData("Hard")]
    public void Validate_ValidDifficulty_ShouldPass(string difficulty)
    {
        // Arrange
        var command = new CreateMissionCommand("Title", "Description", difficulty, 30, "Treasure");

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Treasure")]
    [InlineData("Trivia")]
    public void Validate_ValidType_ShouldPass(string type)
    {
        // Arrange
        var command = new CreateMissionCommand("Title", "Description", "Easy", 30, type);

        // Act
        var result = _sut.TestValidate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}