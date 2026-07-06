using FluentAssertions;
using FluentValidation.TestHelper;
using Sessions.Application.Sessions.Create;
using Xunit;

namespace Sessions.Application.Tests.Sessions.Create;

public class CreateSessionCommandValidatorTests
{
    private readonly CreateSessionCommandValidator _sut = new();

    private static StageInput ValidStage(int order = 1, Guid? missionId = null)
        => new(missionId ?? Guid.NewGuid(), "Trivia Facil", "Trivia", order);

    [Fact]
    public void Validate_ValidCommand_ShouldPass()
    {
        var command = new CreateSessionCommand("Test Session", new List<StageInput> { ValidStage(1) });

        var result = _sut.TestValidate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyName_ShouldFail()
    {
        var command = new CreateSessionCommand("", new List<StageInput> { ValidStage(1) });

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name is required");
    }

    [Fact]
    public void Validate_NameExceeds200Characters_ShouldFail()
    {
        var longName = new string('A', 201);
        var command = new CreateSessionCommand(longName, new List<StageInput> { ValidStage(1) });

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Name must not exceed 200 characters");
    }

    [Fact]
    public void Validate_NameWith200Characters_ShouldPass()
    {
        var name200 = new string('A', 200);
        var command = new CreateSessionCommand(name200, new List<StageInput> { ValidStage(1) });

        var result = _sut.TestValidate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NullStages_ShouldFail()
    {
        var command = new CreateSessionCommand("Test Session", null!);

        var result = _sut.TestValidate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_EmptyStages_ShouldFail()
    {
        var command = new CreateSessionCommand("Test Session", new List<StageInput>());

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Stages)
            .WithErrorMessage("At least one stage is required");
    }

    [Fact]
    public void Validate_StageWithEmptyMissionId_ShouldFail()
    {
        var command = new CreateSessionCommand("Test Session", new List<StageInput> { ValidStage(1, Guid.Empty) });

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Stages[0].MissionId");
    }

    [Fact]
    public void Validate_StageWithEmptyMissionType_ShouldFail()
    {
        var command = new CreateSessionCommand(
            "Test Session",
            new List<StageInput> { new(Guid.NewGuid(), "Title", "", 1) });

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Stages[0].MissionType");
    }

    [Fact]
    public void Validate_DuplicateStageOrder_ShouldFail()
    {
        var command = new CreateSessionCommand(
            "Test Session",
            new List<StageInput>
            {
                ValidStage(1),
                ValidStage(1)
            });

        var result = _sut.TestValidate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("unique"));
    }

    [Fact]
    public void Validate_NonSequentialStageOrder_ShouldFail()
    {
        var command = new CreateSessionCommand(
            "Test Session",
            new List<StageInput>
            {
                ValidStage(1),
                ValidStage(3) // missing 2
            });

        var result = _sut.TestValidate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("sequential"));
    }

    [Fact]
    public void Validate_StageWithZeroOrder_ShouldFail()
    {
        var command = new CreateSessionCommand(
            "Test Session",
            new List<StageInput>
            {
                new(Guid.NewGuid(), "Title", "Trivia", 0)
            });

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor("Stages[0].Order");
    }

    [Theory]
    [InlineData("Session A")]
    [InlineData("A")]
    [InlineData("Special Characters: !@#$%")]
    public void Validate_ValidNames_ShouldPass(string name)
    {
        var command = new CreateSessionCommand(name, new List<StageInput> { ValidStage(1) });

        var result = _sut.TestValidate(command);

        result.IsValid.Should().BeTrue();
    }
}

