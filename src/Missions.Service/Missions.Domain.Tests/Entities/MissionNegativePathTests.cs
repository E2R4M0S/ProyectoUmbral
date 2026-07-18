using FluentAssertions;
using Missions.Domain.Entities;
using Missions.Domain.Enums;
using Xunit;

namespace Missions.Domain.Tests.Entities;

public class MissionNegativePathTests
{
    private static Mission CreateMission() =>
        Mission.Create("Escape del Aula", "Descripcion", Difficulty.Easy, 30, MissionType.Treasure);

    [Fact]
    public void SetStatus_WhenAlreadyInTargetStatus_ShouldThrow()
    {
        var mission = CreateMission();

        var act = () => mission.SetStatus(MissionStatus.Draft);

        act.Should().Throw<InvalidOperationException>().WithMessage("*already in*Draft*");
    }

    [Fact]
    public void SetStatus_FromDraftToInactive_ShouldThrow()
    {
        var mission = CreateMission();

        var act = () => mission.SetStatus(MissionStatus.Inactive);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Draft*Inactive*");
    }

    [Fact]
    public void AddStage_WithDuplicateOrder_ShouldThrow()
    {
        var mission = CreateMission();
        mission.AddStage("Etapa 1", "Desc", 1);

        var act = () => mission.AddStage("Etapa 2", "Desc", 1);

        act.Should().Throw<InvalidOperationException>().WithMessage("*orden 1*");
    }

    [Fact]
    public void AddStage_WithDuplicateName_ShouldThrow()
    {
        var mission = CreateMission();
        mission.AddStage("Etapa 1", "Desc", 1);

        var act = () => mission.AddStage("etapa 1", "Otra desc", 2);

        act.Should().Throw<InvalidOperationException>().WithMessage("*etapa 1*");
    }

    [Fact]
    public void UpdateStage_WhenStageNotFound_ShouldThrow()
    {
        var mission = CreateMission();

        var act = () => mission.UpdateStage(Guid.NewGuid(), "Nueva", "Desc", 1);

        act.Should().Throw<InvalidOperationException>().WithMessage("*not found*");
    }

    [Fact]
    public void UpdateStage_WithDuplicateOrderFromAnotherStage_ShouldThrow()
    {
        var mission = CreateMission();
        mission.AddStage("Etapa 1", "Desc", 1);
        mission.AddStage("Etapa 2", "Desc", 2);
        var stage2Id = mission.Stages.Single(s => s.Order == 2).Id;

        var act = () => mission.UpdateStage(stage2Id, "Etapa 2", "Desc", 1);

        act.Should().Throw<InvalidOperationException>().WithMessage("*orden 1*");
    }

    [Fact]
    public void UpdateStage_WithDuplicateNameFromAnotherStage_ShouldThrow()
    {
        var mission = CreateMission();
        mission.AddStage("Etapa 1", "Desc", 1);
        mission.AddStage("Etapa 2", "Desc", 2);
        var stage2Id = mission.Stages.Single(s => s.Order == 2).Id;

        var act = () => mission.UpdateStage(stage2Id, "etapa 1", "Desc", 2);

        act.Should().Throw<InvalidOperationException>().WithMessage("*etapa 1*");
    }

    [Fact]
    public void AddStageClue_WhenStageNotFound_ShouldThrow()
    {
        var mission = CreateMission();

        var act = () => mission.AddStageClue(Guid.NewGuid(), "Pista", 0, ReleaseType.Manual);

        act.Should().Throw<InvalidOperationException>().WithMessage("*not found*");
    }

    [Fact]
    public void RemoveStageClue_WhenStageNotFound_ShouldThrow()
    {
        var mission = CreateMission();

        var act = () => mission.RemoveStageClue(Guid.NewGuid(), Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>().WithMessage("*not found*");
    }

    [Fact]
    public void RemoveStageClue_WhenActiveAndRemovingLastClue_ShouldThrow()
    {
        var mission = CreateMission();
        mission.AddStage("Etapa 1", "Desc", 1);
        var stage = mission.Stages[0];
        mission.AddStageClue(stage.Id, "Unica pista", 0, ReleaseType.Manual);
        var clueId = stage.Clues[0].Id;
        mission.SetStatus(MissionStatus.Active);

        var act = () => mission.RemoveStageClue(stage.Id, clueId);

        act.Should().Throw<InvalidOperationException>().WithMessage("*last clue*Active*");
    }

    [Fact]
    public void Validate_WithEmptyTitle_ShouldReturnError()
    {
        var mission = Mission.Create("Temp", "Descripcion", Difficulty.Easy, 30, MissionType.Treasure);
        var titleField = typeof(Mission).GetProperty("Title")!;
        titleField.SetValue(mission, "");

        var result = mission.Validate();

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Mission title cannot be empty");
    }

    [Fact]
    public void Validate_WithEmptyDescription_ShouldReturnError()
    {
        var mission = Mission.Create("Titulo", "Temp", Difficulty.Easy, 30, MissionType.Treasure);
        var descriptionField = typeof(Mission).GetProperty("Description")!;
        descriptionField.SetValue(mission, "  ");

        var result = mission.Validate();

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Mission description cannot be empty");
    }
}
