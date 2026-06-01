using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.Create;
using Sessions.Domain.Entities;
using Xunit;

namespace Sessions.Application.Tests.Sessions.Create;

public class CreateSessionCommandHandlerTests
{
    private readonly ISessionRepository _repository = Substitute.For<ISessionRepository>();
    private readonly ILogger<CreateSessionCommandHandler> _logger =
        Substitute.For<ILogger<CreateSessionCommandHandler>>();
    private readonly CreateSessionCommandHandler _sut;

    public CreateSessionCommandHandlerTests()
    {
        _sut = new CreateSessionCommandHandler(_repository, _logger);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateSessionWithSixDigitPin()
    {
        // Arrange
        var command = new CreateSessionCommand("Test Session", Guid.NewGuid(), "Test Mission");

        _repository.IsPinUniqueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _repository.AddAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test Session");
        result.MissionId.Should().Be(command.MissionId);
        result.Pin.Should().HaveLength(6);
        result.Pin.Should().MatchRegex(@"^\d{6}$");
        result.Status.Should().Be("Scheduled");
        result.StartedAt.Should().BeNull();
        result.EndedAt.Should().BeNull();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        await _repository.Received(1).AddAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPinCollisionOccurs_ShouldRegeneratePinAndSucceed()
    {
        // Arrange
        var command = new CreateSessionCommand("Collision Session", Guid.NewGuid(), "Test Mission");

        _repository.IsPinUniqueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false, false, false, false, false, false, false, false, false, true);

        _repository.AddAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Pin.Should().HaveLength(6);
        // Should have checked PIN 10 times (9 collisions + 1 success)
        await _repository.Received(10).IsPinUniqueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPinCollisionExceedsMaxAttempts_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var command = new CreateSessionCommand("Max Collision Session", Guid.NewGuid(), "Test Mission");

        // Always return false (always collision)
        _repository.IsPinUniqueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Could not generate unique PIN after 10 attempts*");

        // Should have tried exactly 10 times
        await _repository.Received(10).IsPinUniqueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().AddAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldLogSessionCreation()
    {
        // Arrange
        var command = new CreateSessionCommand("Log Test Session", Guid.NewGuid(), "Test Mission");

        _repository.IsPinUniqueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        _repository.AddAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(v => v.ToString()!.Contains("Session created")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }
}
