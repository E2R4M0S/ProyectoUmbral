using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.Join;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;
using Xunit;

namespace Sessions.Application.Tests.Sessions.Join;

public class JoinSessionCommandHandlerTests
{
    private readonly ISessionRepository _repository = Substitute.For<ISessionRepository>();
    private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
    private readonly ILogger<JoinSessionCommandHandler> _logger =
        Substitute.For<ILogger<JoinSessionCommandHandler>>();
    private readonly JoinSessionCommandHandler _sut;

    public JoinSessionCommandHandlerTests()
    {
        _sut = new JoinSessionCommandHandler(_repository, _httpContextAccessor, _logger);
    }

    [Fact]
    public async Task Handle_WithValidPinAndInPreparationStatus_ShouldAddParticipant()
    {
        // Arrange
        var session = CreateSessionWithStatus(SessionStatus.Preparing);
        var userId = Guid.NewGuid();
        var command = new JoinSessionCommand("123456");

        SetupUserId(userId);
        _repository.GetByPinAsync("123456", Arg.Any<CancellationToken>())
            .Returns(session);
        _repository.AddParticipantAsync(Arg.Any<SessionParticipant>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().Be(session.Id);
        result.UserId.Should().Be(userId);
        result.JoinedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        await _repository.Received(1).AddParticipantAsync(Arg.Any<SessionParticipant>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithInvalidPin_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var command = new JoinSessionCommand("000000");

        _repository.GetByPinAsync("000000", Arg.Any<CancellationToken>())
            .Returns((Session?)null);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Session with PIN*not found*");
    }

    [Fact]
    public async Task Handle_WithSessionNotInPreparationStatus_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateSessionWithStatus(SessionStatus.Scheduled);
        var command = new JoinSessionCommand("123456");

        _repository.GetByPinAsync("123456", Arg.Any<CancellationToken>())
            .Returns(session);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cannot join session in 'Scheduled' status*");
    }

    [Fact]
    public async Task Handle_WhenParticipantAlreadyJoined_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var session = CreateSessionWithStatus(SessionStatus.Preparing);
        var userId = Guid.NewGuid();

        // Use reflection to add a participant directly to the session's private collection
        var participantsField = typeof(Session).GetField("_participants",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var participants = (List<SessionParticipant>)participantsField!.GetValue(session)!;
        participants.Add(SessionParticipant.Create(session.Id, userId));

        var command = new JoinSessionCommand("123456");
        SetupUserId(userId);

        _repository.GetByPinAsync("123456", Arg.Any<CancellationToken>())
            .Returns(session);
        _repository.AddParticipantAsync(Arg.Any<SessionParticipant>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert — handler no longer throws for duplicate participants; it allows re-join
        result.Should().NotBeNull();
        result.SessionId.Should().Be(session.Id);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldLogSessionJoin()
    {
        // Arrange
        var session = CreateSessionWithStatus(SessionStatus.Preparing);
        var userId = Guid.NewGuid();
        var command = new JoinSessionCommand("123456");

        SetupUserId(userId);
        _repository.GetByPinAsync("123456", Arg.Any<CancellationToken>())
            .Returns(session);
        _repository.AddParticipantAsync(Arg.Any<SessionParticipant>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(v => v.ToString()!.Contains("joined session")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    private void SetupUserId(Guid userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        _httpContextAccessor.HttpContext = httpContext;
    }

    private static Session CreateSessionWithStatus(SessionStatus status)
    {
        var session = Session.Create("Test Session", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), "Test Mission", "Trivia", 1) });
        var statusProperty = typeof(Session).GetProperty("Status")!;
        statusProperty.SetValue(session, status);
        return session;
    }
}