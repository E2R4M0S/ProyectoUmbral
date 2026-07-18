using FluentAssertions;
using MediatR;
using NSubstitute;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.Transition;
using Sessions.Infrastructure.Services;
using Xunit;

namespace Sessions.Service.Sessions.Infrastructure.Tests.Services;

public class GameSessionFacadeTests
{
    private readonly IMediator _mediator;
    private readonly IGameNotifier _notifier;
    private readonly GameSessionFacade _facade;

    public GameSessionFacadeTests()
    {
        _mediator = Substitute.For<IMediator>();
        _notifier = Substitute.For<IGameNotifier>();
        _facade = new GameSessionFacade(_mediator, _notifier);
    }

    [Fact]
    public async Task TransitionAndNotify_SendsCommandAndNotification()
    {
        var sessionId = Guid.NewGuid();
        var newStatus = "Active";

        await _facade.TransitionAndNotify(sessionId, newStatus);

        await _mediator.Received().Send(
            Arg.Is<TransitionSessionCommand>(c => c.Id == sessionId && c.NewStatus == newStatus),
            Arg.Any<CancellationToken>());
        await _notifier.Received().NotifySessionStatusChanged(sessionId, newStatus, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TransitionAndNotify_PropagatesCancellation()
    {
        var sessionId = Guid.NewGuid();
        var cts = new CancellationTokenSource();

        await _facade.TransitionAndNotify(sessionId, "Paused", cts.Token);

        await _mediator.Received().Send(
            Arg.Any<TransitionSessionCommand>(),
            cts.Token);
    }

    [Fact]
    public async Task ReleaseClueAndNotify_SendsNotification()
    {
        var sessionId = Guid.NewGuid();
        var clueId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var clueContent = "Look behind the painting";
        var penalty = 10;

        await _facade.ReleaseClueAndNotify(sessionId, clueId, teamId, null, clueContent, penalty);

        await _notifier.Received().NotifyClueReleased(
            sessionId,
            teamId,
            null,
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReleaseClueAndNotify_WithoutContent_UsesDefaultText()
    {
        var sessionId = Guid.NewGuid();
        var clueId = Guid.NewGuid();

        await _facade.ReleaseClueAndNotify(sessionId, clueId, null, null, null, null);

        await _notifier.Received().NotifyClueReleased(
            sessionId,
            null,
            null,
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReleaseClueAndNotify_WithoutTeamId_SendsNullTeamId()
    {
        var sessionId = Guid.NewGuid();
        var clueId = Guid.NewGuid();

        await _facade.ReleaseClueAndNotify(sessionId, clueId, null, null, "Some clue", 5);

        await _notifier.Received().NotifyClueReleased(
            sessionId,
            null,
            null,
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReleaseClueAndNotify_WithUserId_SendsUserId()
    {
        var sessionId = Guid.NewGuid();
        var clueId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await _facade.ReleaseClueAndNotify(sessionId, clueId, null, userId, "Just for you", 5);

        await _notifier.Received().NotifyClueReleased(
            sessionId,
            null,
            userId,
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotifyStageAdvanced_CallsProgressUpdated_WithStageAdvancedFlag()
    {
        var sessionId = Guid.NewGuid();
        var newStageOrder = 2;

        await _facade.NotifyStageAdvanced(sessionId, newStageOrder);

        await _notifier.Received().NotifyProgressUpdated(
            sessionId,
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }
}