using FluentAssertions;
using Missions.Application.Common.Interfaces;
using Missions.Application.Common.Proxy;
using Missions.Domain.Entities;
using Missions.Domain.Enums;
using NSubstitute;
using Xunit;

namespace Missions.Application.Tests.Proxy;

public class MissionAccessProxyTests
{
    private readonly IMissionRepository _inner = Substitute.For<IMissionRepository>();
    private readonly MissionAccessProxy _proxy;
    private readonly Mission _draftMission;
    private readonly Mission _activeMission;

    public MissionAccessProxyTests()
    {
        _proxy = new MissionAccessProxy(_inner);
        _draftMission = Mission.Create("Test", "Desc", Difficulty.Medium, 30, MissionType.Treasure);
        _activeMission = Mission.Create("Test2", "Desc2", Difficulty.Medium, 30, MissionType.Treasure);
        _activeMission.AddStage("Stage 1", "Desc", 1);
        _activeMission.SetStatus(MissionStatus.Active);
    }

    [Fact]
    public async Task GetById_ShouldPassThrough()
    {
        await _proxy.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);
        await _inner.Received(1).GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Add_ShouldPassThrough()
    {
        await _proxy.AddAsync(_draftMission, CancellationToken.None);
        await _inner.Received(1).AddAsync(_draftMission, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_ActiveMission_ShouldThrow()
    {
        var act = () => _proxy.UpdateAsync(_activeMission, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Active*");
    }

    [Fact]
    public async Task AddStage_ActiveMission_ShouldThrow()
    {
        var act = () => _proxy.AddStageAsync(_activeMission, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task AddClue_ActiveMission_ShouldThrow()
    {
        var act = () => _proxy.AddClueAsync(_activeMission, Guid.NewGuid(), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Update_DraftMission_ShouldPass()
    {
        await _proxy.UpdateAsync(_draftMission, CancellationToken.None);
        await _inner.Received(1).UpdateAsync(_draftMission, Arg.Any<CancellationToken>());
    }
}
