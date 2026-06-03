using Xunit;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Microsoft.Extensions.Logging;
using Trivia.Application.Common.Interfaces;
using Trivia.Application.Trivias.Clues;

namespace Trivia.Application.Tests.Trivias.Clues;

public class ReleaseClueCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldPublishClueReleasedEvent()
    {
        var publisher = Substitute.For<IEventPublisher>();
        var logger = Substitute.For<ILogger<ReleaseClueCommandHandler>>();
        var handler = new ReleaseClueCommandHandler(publisher, logger);

        var cmd = new ReleaseClueCommand(
            QuizId: Guid.NewGuid(),
            TeamId: Guid.NewGuid(),
            ClueData: new { Text = "Test clue" });

        await handler.Handle(cmd, CancellationToken.None);

        await publisher.Received(1).PublishAsync(
            Arg.Is<string>(s => s == "ClueReleased"),
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }
}
