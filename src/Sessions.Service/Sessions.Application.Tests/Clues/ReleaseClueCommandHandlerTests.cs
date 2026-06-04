using FluentAssertions;
using NSubstitute;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.Clues;
using Xunit;

namespace Sessions.Application.Tests.Clues;

public class ReleaseClueCommandHandlerTests
{
    private readonly IGameNotifier _notifier = Substitute.For<IGameNotifier>();
    private readonly ReleaseClueCommandHandler _sut;

    public ReleaseClueCommandHandlerTests()
    {
        _sut = new ReleaseClueCommandHandler(_notifier);
    }

    [Fact]
    public async Task Handle_ShouldNotifyClueReleased()
    {
        var sessionId = Guid.NewGuid();
        var clueId = Guid.NewGuid();
        var command = new ReleaseClueCommand(sessionId, clueId, null);

        await _sut.Handle(command, CancellationToken.None);

        await _notifier.Received(1).NotifyClueReleased(
            sessionId, null, Arg.Any<object>(), Arg.Any<CancellationToken>());
    }
}
