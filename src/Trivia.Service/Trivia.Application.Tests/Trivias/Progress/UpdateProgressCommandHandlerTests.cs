using Xunit;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using Microsoft.Extensions.Logging;
using Trivia.Application.Common.Interfaces;
using Trivia.Application.Trivias.Progress;

namespace Trivia.Application.Tests.Trivias.Progress;

public class UpdateProgressCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldPublishProgressUpdatedEvent()
    {
        var publisher = Substitute.For<IEventPublisher>();
        var logger = Substitute.For<ILogger<UpdateProgressCommandHandler>>();
        var handler = new UpdateProgressCommandHandler(publisher, logger);

        var cmd = new UpdateProgressCommand(
            QuizId: Guid.NewGuid(),
            ProgressData: new { ElapsedSeconds = 30 });

        await handler.Handle(cmd, CancellationToken.None);

        await publisher.Received(1).PublishAsync(
            Arg.Is<string>(s => s == "ProgressUpdated"),
            Arg.Any<object>(),
            Arg.Any<CancellationToken>());
    }
}
