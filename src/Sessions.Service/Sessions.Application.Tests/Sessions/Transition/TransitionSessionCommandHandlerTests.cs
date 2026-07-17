using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
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
    private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
    private readonly ILogger<TransitionSessionCommandHandler> _logger =
        Substitute.For<ILogger<TransitionSessionCommandHandler>>();
    private readonly IEventPublisher _publisher = Substitute.For<IEventPublisher>();
    private readonly TransitionSessionCommandHandler _sut;

    public TransitionSessionCommandHandlerTests()
    {
        // Explicit null HttpContext — mirrors a system/background caller, so the RB-10
        // ownership check is skipped and these pre-existing tests keep their original behavior.
        // (NSubstitute auto-substitutes a non-null HttpContext for unconfigured members since
        // HttpContext is a non-sealed class, so this must be set explicitly rather than relying
        // on the mock's default.)
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        _sut = new TransitionSessionCommandHandler(_repository, _httpContextAccessor, _logger, _publisher);
    }

    [Fact]
    public async Task Handle_WithValidTransition_ShouldUpdateSession()
    {
        // Arrange
        var session = CreateSessionWithStatus(SessionStatus.Preparing);
        session.AddParticipant(Guid.NewGuid());
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
        await _repository.Received(1).AddAuditEventAsync(
            Arg.Is<SessionAuditEvent>(e => e.SessionId == session.Id && e.EventType == SessionAuditEventTypes.StatusChanged),
            Arg.Any<CancellationToken>());
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
        // Arrange — target isn't Active, so this exercises the state-machine check
        // rather than the HasParticipantsHandler guard.
        var session = CreateSessionWithStatus(SessionStatus.Scheduled);
        var command = new TransitionSessionCommand(session.Id, "Finished");

        _repository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(session);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot transition session from 'Scheduled' to 'Finished'*");
    }

    [Fact]
    public async Task Handle_WithValidTransition_ShouldLogSessionTransition()
    {
        // Arrange
        var session = CreateSessionWithStatus(SessionStatus.Preparing);
        session.AddParticipant(Guid.NewGuid());
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

    private static Session CreateSessionWithStatus(SessionStatus status, Guid? operatorId = null)
    {
        var session = Session.Create("Test Session", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Mission", "Stage", "Trivia", 1, "test-token") }, operatorId);
        var statusProperty = typeof(Session).GetProperty("Status")!;
        statusProperty.SetValue(session, status);
        return session;
    }

    private void SetAuthenticatedUser(Guid userId)
    {
        var identity = new ClaimsIdentity(new[] { new Claim("sub", userId.ToString()) }, "Test");
        _httpContextAccessor.HttpContext.Returns(new DefaultHttpContext { User = new ClaimsPrincipal(identity) });
    }

    [Fact]
    public async Task Handle_WhenCalledByOwningOperator_ShouldSucceed()
    {
        var operatorId = Guid.NewGuid();
        var session = CreateSessionWithStatus(SessionStatus.Preparing, operatorId);
        session.AddParticipant(Guid.NewGuid());
        SetAuthenticatedUser(operatorId);
        var command = new TransitionSessionCommand(session.Id, "Active");

        _repository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _repository.UpdateAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await _sut.Handle(command, CancellationToken.None);

        session.Status.Should().Be(SessionStatus.Active);
    }

    [Fact]
    public async Task Handle_WhenCalledByNonOwningOperator_ShouldThrowUnauthorized()
    {
        var operatorId = Guid.NewGuid();
        var someoneElse = Guid.NewGuid();
        var session = CreateSessionWithStatus(SessionStatus.Preparing, operatorId);
        session.AddParticipant(Guid.NewGuid());
        SetAuthenticatedUser(someoneElse);
        var command = new TransitionSessionCommand(session.Id, "Active");

        _repository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSessionHasNoOperator_ShouldThrowUnauthorizedEvenForAuthenticatedOperator()
    {
        // RB-10: sessions predating OperatorId (null) cannot be managed by anyone.
        var session = CreateSessionWithStatus(SessionStatus.Preparing, operatorId: null);
        session.AddParticipant(Guid.NewGuid());
        SetAuthenticatedUser(Guid.NewGuid());
        var command = new TransitionSessionCommand(session.Id, "Active");

        _repository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}

