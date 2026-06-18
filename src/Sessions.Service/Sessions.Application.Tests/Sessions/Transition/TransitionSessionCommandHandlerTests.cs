using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.Transition;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;
using Xunit;

namespace Sessions.Application.Tests.Sessions.Transition;

public class TransitionSessionCommandHandlerTests
{
    private readonly ISessionRepository _repository = Substitute.For<ISessionRepository>();
    private readonly ILogger<TransitionSessionCommandHandler> _logger =
        Substitute.For<ILogger<TransitionSessionCommandHandler>>();
    private readonly IEventPublisher _publisher = Substitute.For<IEventPublisher>();
    private readonly TransitionSessionCommandHandler _sut;

    public TransitionSessionCommandHandlerTests()
    {
        _sut = new TransitionSessionCommandHandler(_repository, _logger, _publisher);
    }

    [Fact]
    public async Task Handle_WithValidTransition_ShouldUpdateSession()
    {
        // Arrange
        var session = CreateSessionWithStatus(SessionStatus.Preparing);
        var command = new TransitionSessionCommand(session.Id, "Active");

        _repository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(session);
        _repository.UpdateAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        session.Status.Should().Be(SessionStatus.Active);
        await _repository.Received(1).UpdateAsync(session, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSessionNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var command = new TransitionSessionCommand(sessionId, "Active");

        _repository.GetByIdAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns((Session?)null);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Session with id '{sessionId}' not found*");
    }

    [Fact]
    public async Task Handle_WithInvalidTransition_ShouldPropagateException()
    {
        // Arrange
        var session = CreateSessionWithStatus(SessionStatus.Scheduled);
        var command = new TransitionSessionCommand(session.Id, "Active");

        _repository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(session);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot transition session from 'Scheduled' to 'Active'*");
    }

    [Fact]
    public async Task Handle_WithValidTransition_ShouldLogSessionTransition()
    {
        // Arrange
        var session = CreateSessionWithStatus(SessionStatus.Preparing);
        var command = new TransitionSessionCommand(session.Id, "Active");

        _repository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(session);
        _repository.UpdateAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(v => v.ToString()!.Contains("Session status transitioned")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    private static Session CreateSessionWithStatus(SessionStatus status)
    {
        var session = Session.Create("Test Session", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), "Test Mission", "Trivia", 1) });
        var statusProperty = typeof(Session).GetProperty("Status")!;
        statusProperty.SetValue(session, status);
        return session;
    }
}
