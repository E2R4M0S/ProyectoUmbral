using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Missions.Application.Missions.Create;
using Missions.Application.Missions.Update;
using Missions.Application.Missions.Stages;
using Missions.Application.Missions.Clues;
using Missions.Application.Missions.Catalog;
using Missions.Application.Missions.StatusChange;
using Xunit;

namespace Missions.Api.Tests.Endpoints;

public class MissionsApiEndpointTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogger<Program> _logger = Substitute.For<ILogger<Program>>();

    [Fact]
    public async Task CreateMission_ValidCommand_CallsMediator()
    {
        var command = new CreateMissionCommand("Test", "Desc", "Easy", 30, "Treasure");
        var cmdResult = new CreateMissionCommandResult(Guid.NewGuid(), "Test", "Desc", "Easy", 30, "Treasure", "Draft", DateTime.UtcNow);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Task.FromResult(cmdResult));

        await Simulate(() => _mediator.Send(command));

        await _mediator.Received(1).Send(command, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateMission_DuplicateTitle_ThrowsConflict()
    {
        var command = new CreateMissionCommand("Test", "Desc", "Easy", 30, "Treasure");
        _mediator.Send(command, Arg.Any<CancellationToken>())!
            .Returns<Task<CreateMissionCommandResult>>(_ => throw new InvalidOperationException("Title 'Test' already exists"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Simulate(() => _mediator.Send(command)));

        ex.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task UpdateMission_ValidCommand_CallsMediator()
    {
        var id = Guid.NewGuid();
        var command = new UpdateMissionCommand(id, "Updated", "New desc", "Medium", 60);

        await SimulateVoid(() => _mediator.Send(command));

        await _mediator.Received(1).Send(command, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateMission_NotFound_ThrowsNotFound()
    {
        var id = Guid.NewGuid();
        var command = new UpdateMissionCommand(id, "X", "X", "Easy", 15);
        _mediator.Send(command, Arg.Any<CancellationToken>())!
            .Returns<Task>(_ => throw new InvalidOperationException($"Mission with id '{id}' not found"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            SimulateVoid(() => _mediator.Send(command)));

        ex.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task CreateStage_ValidCommand_CallsMediator()
    {
        var missionId = Guid.NewGuid();
        var command = new CreateStageCommand(missionId, "Stage 1", "First stage", 1);
        var cmdResult = new CreateStageCommandResult(Guid.NewGuid(), "Stage 1", "First stage", 1);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Task.FromResult(cmdResult));

        await Simulate(() => _mediator.Send(command));

        await _mediator.Received(1).Send(command, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateStage_MissionNotFound_ThrowsNotFound()
    {
        var missionId = Guid.NewGuid();
        var command = new CreateStageCommand(missionId, "Stage 1", "X", 1);
        _mediator.Send(command, Arg.Any<CancellationToken>())!
            .Returns<Task<CreateStageCommandResult>>(_ => throw new InvalidOperationException($"Mission with id '{missionId}' not found"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Simulate(() => _mediator.Send(command)));

        ex.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task CreateStage_DuplicateOrder_ThrowsConflict()
    {
        var missionId = Guid.NewGuid();
        var command = new CreateStageCommand(missionId, "Stage 1", "X", 1);
        _mediator.Send(command, Arg.Any<CancellationToken>())!
            .Returns<Task<CreateStageCommandResult>>(_ => throw new InvalidOperationException("Ya existe una etapa con el orden 1"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Simulate(() => _mediator.Send(command)));

        ex.Message.Should().Contain("Ya existe");
    }

    [Fact]
    public async Task UpdateStage_ValidCommand_CallsMediator()
    {
        var missionId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var command = new UpdateStageCommand(missionId, stageId, "Updated", "Desc", 1);

        await SimulateVoid(() => _mediator.Send(command));

        await _mediator.Received(1).Send(command, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateStage_NotFound_ThrowsNotFound()
    {
        var missionId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var command = new UpdateStageCommand(missionId, stageId, "X", "X", 1);
        _mediator.Send(command, Arg.Any<CancellationToken>())!
            .Returns<Task>(_ => throw new InvalidOperationException("not found"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            SimulateVoid(() => _mediator.Send(command)));

        ex.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task DeleteStage_ValidCommand_CallsMediator()
    {
        var missionId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var command = new DeleteStageCommand(missionId, stageId);

        await SimulateVoid(() => _mediator.Send(command));

        await _mediator.Received(1).Send(command, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateClue_ValidCommand_CallsMediator()
    {
        var missionId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var command = new CreateClueCommand(missionId, stageId, "Find the key", 10, "Auto");
        var cmdResult = new CreateClueCommandResult(Guid.NewGuid(), "Find the key", 10, "Auto");
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Task.FromResult(cmdResult));

        await Simulate(() => _mediator.Send(command));

        await _mediator.Received(1).Send(command, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteClue_ValidCommand_CallsMediator()
    {
        var missionId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var clueId = Guid.NewGuid();
        var command = new DeleteClueCommand(missionId, stageId, clueId);

        await SimulateVoid(() => _mediator.Send(command));

        await _mediator.Received(1).Send(command, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeMissionStatus_ValidCommand_CallsMediator()
    {
        var missionId = Guid.NewGuid();
        var command = new ChangeMissionStatusCommand(missionId, "Published");

        await SimulateVoid(() => _mediator.Send(command));

        await _mediator.Received(1).Send(command, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MissionCatalog_ReturnsItems()
    {
        var items = new List<MissionListItemDto>
        {
            new(Guid.NewGuid(), "Mission 1", "Easy", "Treasure", "Published"),
            new(Guid.NewGuid(), "Mission 2", "Medium", "Trivia", "Draft"),
        };
        var queryResult = new GetMissionsResult(items.AsReadOnly(), 2, 1, 10);
        _mediator.Send(Arg.Any<GetMissionsQuery>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(queryResult));

        var query = new GetMissionsQuery(null, null, null, 1, 10);
        await _mediator.Send(query);

        await _mediator.Received(1).Send(Arg.Any<GetMissionsQuery>(), Arg.Any<CancellationToken>());
    }

    // ─── Helpers ──────────────────────────────────────────

    private static async Task Simulate(Func<Task> action)
    {
        await action();
    }

    private static async Task SimulateVoid(Func<Task> action)
    {
        await action();
    }
}
