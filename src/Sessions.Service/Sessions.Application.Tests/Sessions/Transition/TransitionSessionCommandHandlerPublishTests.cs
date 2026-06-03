using System.Threading;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.Transition;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;
using Xunit;

namespace Sessions.Application.Tests.Sessions.Transition;

public class TransitionSessionCommandHandlerPublishTests
{
    private readonly ISessionRepository _repository = Substitute.For<ISessionRepository>();
    private readonly ILogger<TransitionSessionCommandHandler> _logger =
        Substitute.For<ILogger<TransitionSessionCommandHandler>>();
    private readonly IEventPublisher _publisher = Substitute.For<IEventPublisher>();
    private readonly TransitionSessionCommandHandler _sut;

    public TransitionSessionCommandHandlerPublishTests()
    {
        _sut = new TransitionSessionCommandHandler(_repository, _logger, _publisher);
    }

    [Fact]
    public async Task Handle_WhenSessionFinished_ShouldPublishEvent()
    {
        var session = CreateSessionWithStatus(SessionStatus.Active);
        var command = new TransitionSessionCommand(session.Id, "Finished");

        _repository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(session);
        _repository.UpdateAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await _sut.Handle(command, CancellationToken.None);

        await _publisher.Received(1).PublishAsync(Arg.Is<string>(s => s == "session.status.changed"), Arg.Any<object>());
    }

    private static Session CreateSessionWithStatus(SessionStatus status)
    {
        var session = Session.Create("Test Session", Guid.NewGuid(), "Test Mission", "123456");
        var statusProperty = typeof(Session).GetProperty("Status")!;
        statusProperty.SetValue(session, status);
        return session;
    }
}
