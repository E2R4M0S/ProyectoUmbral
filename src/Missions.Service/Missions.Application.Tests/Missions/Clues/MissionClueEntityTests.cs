using FluentAssertions;
using Missions.Domain.Entities;
using Missions.Domain.Enums;
using Xunit;

namespace Missions.Application.Tests.Missions.Clues;

public class MissionClueEntityTests
{
    [Fact]
    public void MissionClue_ShouldHaveIdProperty()
    {
        var idProp = typeof(MissionClue).GetProperty("Id");
        idProp.Should().NotBeNull();
        idProp!.PropertyType.Should().Be(typeof(Guid));
    }

    [Fact]
    public void MissionClue_ShouldHaveStageIdProperty()
    {
        var stageIdProp = typeof(MissionClue).GetProperty("StageId");
        stageIdProp.Should().NotBeNull();
        stageIdProp!.PropertyType.Should().Be(typeof(Guid));
    }

    [Fact]
    public void MissionClue_ShouldHaveContentProperty()
    {
        var contentProp = typeof(MissionClue).GetProperty("Content");
        contentProp.Should().NotBeNull();
        contentProp!.PropertyType.Should().Be(typeof(string));
    }

    [Fact]
    public void MissionClue_ShouldHavePenaltyProperty()
    {
        var penaltyProp = typeof(MissionClue).GetProperty("Penalty");
        penaltyProp.Should().NotBeNull();
        penaltyProp!.PropertyType.Should().Be(typeof(int?));
    }

    [Fact]
    public void MissionClue_ShouldHaveReleaseTypeProperty()
    {
        var releaseTypeProp = typeof(MissionClue).GetProperty("ReleaseType");
        releaseTypeProp.Should().NotBeNull();
        releaseTypeProp!.PropertyType.Should().Be(typeof(ReleaseType));
    }
}
