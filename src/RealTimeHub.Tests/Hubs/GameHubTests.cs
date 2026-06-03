using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using RealTimeHub.Hubs;
using Xunit;

namespace RealTimeHub.Tests.Hubs;

public class GameHubTests
{
    private readonly GameHub _hub;
    private readonly HubCallerContext _mockContext;
    private readonly IGroupManager _mockGroups;

    public GameHubTests()
    {
        _hub = new GameHub();
        _mockContext = Substitute.For<HubCallerContext>();
        _mockGroups = Substitute.For<IGroupManager>();

        _mockContext.ConnectionId.Returns("test-connection-id");
        _hub.Context = _mockContext;
        _hub.Groups = _mockGroups;
    }

    [Fact]
    public async Task JoinSessionGroup_ShouldAddConnectionToGroup()
    {
        var sessionId = "session-abc-123";

        await _hub.JoinSessionGroup(sessionId);

        await _mockGroups.Received(1).AddToGroupAsync(
            "test-connection-id",
            sessionId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LeaveSessionGroup_ShouldRemoveConnectionFromGroup()
    {
        var sessionId = "session-xyz-456";

        await _hub.LeaveSessionGroup(sessionId);

        await _mockGroups.Received(1).RemoveFromGroupAsync(
            "test-connection-id",
            sessionId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinSessionGroup_MultipleCalls_ShouldJoinMultipleGroups()
    {
        await _hub.JoinSessionGroup("group-1");
        await _hub.JoinSessionGroup("group-2");

        await _mockGroups.Received(1).AddToGroupAsync(
            "test-connection-id", "group-1", Arg.Any<CancellationToken>());
        await _mockGroups.Received(1).AddToGroupAsync(
            "test-connection-id", "group-2", Arg.Any<CancellationToken>());
    }
}
