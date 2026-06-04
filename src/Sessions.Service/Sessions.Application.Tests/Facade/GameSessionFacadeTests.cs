using FluentAssertions;
using MediatR;
using NSubstitute;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.Transition;
using Sessions.Infrastructure.Services;
using Xunit;

namespace Sessions.Application.Tests.Facade;

public class GameSessionFacadeTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IGameNotifier _notifier = Substitute.For<IGameNotifier>();
    private readonly GameSessionFacade _facade;

    public GameSessionFacadeTests()
    {
        _facade = new GameSessionFacade(_mediator, _notifier);
    }

    [Fact]
    public async Task TransitionAndNotify_ShouldCallMediatorAndNotifier()
    {
        var sessionId = Guid.NewGuid();
        await _facade.TransitionAndNotify(sessionId, "Active");

        await _mediator.Received(1).Send(
            Arg.Is<TransitionSessionCommand>(c => c.Id == sessionId && c.NewStatus == "Active"),
            Arg.Any<CancellationToken>());
        await _notifier.Received(1).NotifySessionStatusChanged(sessionId, "Active", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReleaseClueAndNotify_ShouldCallNotifier()
    {
        await _facade.ReleaseClueAndNotify(Guid.NewGuid(), Guid.NewGuid(), null, "pista", 5);
        await _notifier.Received(1).NotifyClueReleased(Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }
}
