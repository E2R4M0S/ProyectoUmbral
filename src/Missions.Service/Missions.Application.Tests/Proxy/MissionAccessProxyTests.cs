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
    private readonly MissionStage _stage;

    public MissionAccessProxyTests()
    {
        _proxy = new MissionAccessProxy(_inner);
        _draftMission = Mission.Create("Test Draft", "Desc", Difficulty.Medium, 30, MissionType.Treasure);
        _draftMission.AddStage("Stage 1", "Desc", 1);
        _activeMission = Mission.Create("Test Active", "Desc", Difficulty.Medium, 30, MissionType.Treasure);
        _activeMission.AddStage("Stage 1", "Desc", 1);
        _activeMission.SetStatus(MissionStatus.Active);
        _stage = _activeMission.Stages.First();
    }

    // ═══════════════════════════════════════════════════════════════════
    // READ OPERATIONS — all pass through directly
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetByIdAsync_ShouldPassThrough()
    {
        var id = Guid.NewGuid();
        await _proxy.GetByIdAsync(id, CancellationToken.None);
        await _inner.Received(1).GetByIdAsync(id, CancellationToken.None);
    }

    [Fact]
    public async Task GetMissionsAsync_ShouldPassThrough()
    {
        await _proxy.GetMissionsAsync("search", "Medium", "Draft", 1, 10, CancellationToken.None);
        await _inner.Received(1).GetMissionsAsync("search", "Medium", "Draft", 1, 10, CancellationToken.None);
    }

    [Fact]
    public async Task IsTitleUniqueAsync_ShouldPassThrough()
    {
        await _proxy.IsTitleUniqueAsync("Unique Title", CancellationToken.None);
        await _inner.Received(1).IsTitleUniqueAsync("Unique Title", CancellationToken.None, null);
    }

    [Fact]
    public async Task IsTitleUniqueAsync_WithExcludeId_ShouldPassThrough()
    {
        var excludeId = Guid.NewGuid();
        await _proxy.IsTitleUniqueAsync("Title", CancellationToken.None, excludeId);
        await _inner.Received(1).IsTitleUniqueAsync("Title", CancellationToken.None, excludeId);
    }

    // ═══════════════════════════════════════════════════════════════════
    // CREATE OPERATION — passes through (new missions are Draft, not Active)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AddAsync_ShouldPassThrough()
    {
        await _proxy.AddAsync(_draftMission, CancellationToken.None);
        await _inner.Received(1).AddAsync(_draftMission, Arg.Any<CancellationToken>());
    }

    // ═══════════════════════════════════════════════════════════════════
    // WRITE OPERATIONS — BLOCKED when mission.Status == Active
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateAsync_OnActiveMission_ShouldThrow()
    {
        var act = () => _proxy.UpdateAsync(_activeMission, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Active*");
    }

    [Fact]
    public async Task UpdateAsync_OnDraftMission_ShouldPassThrough()
    {
        await _proxy.UpdateAsync(_draftMission, CancellationToken.None);
        await _inner.Received(1).UpdateAsync(_draftMission, CancellationToken.None);
    }

    [Fact]
    public async Task AddStageAsync_OnActiveMission_ShouldThrow()
    {
        var act = () => _proxy.AddStageAsync(_activeMission, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Active*");
    }

    [Fact]
    public async Task AddStageAsync_OnDraftMission_ShouldPassThrough()
    {
        await _proxy.AddStageAsync(_draftMission, CancellationToken.None);
        await _inner.Received(1).AddStageAsync(_draftMission, CancellationToken.None);
    }

    [Fact]
    public async Task AddClueAsync_OnActiveMission_ShouldThrow()
    {
        var stageId = Guid.NewGuid();
        var act = () => _proxy.AddClueAsync(_activeMission, stageId, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Active*");
    }

    [Fact]
    public async Task AddClueAsync_OnDraftMission_ShouldPassThrough()
    {
        var stageId = Guid.NewGuid();
        await _proxy.AddClueAsync(_draftMission, stageId, CancellationToken.None);
        await _inner.Received(1).AddClueAsync(_draftMission, stageId, CancellationToken.None);
    }

    [Fact]
    public void RemoveStage_OnActiveMission_ShouldThrow()
    {
        var act = () => _proxy.RemoveStage(_activeMission, _stage);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Active*");
    }

    [Fact]
    public void RemoveStage_OnDraftMission_ShouldPassThrough()
    {
        var draftStage = _draftMission.Stages.First();
        _proxy.RemoveStage(_draftMission, draftStage);
        _inner.Received(1).RemoveStage(_draftMission, draftStage);
    }

    // ═══════════════════════════════════════════════════════════════════
    // SAVE CHANGES — call passes through
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SaveChangesAsync_ShouldPassThrough()
    {
        await _proxy.SaveChangesAsync(CancellationToken.None);
        await _inner.Received(1).SaveChangesAsync(CancellationToken.None);
    }
}
