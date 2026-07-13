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

    private static CreateSessionCommand SingleStageCommand(string name = "Test Session")
        => new(name, new List<StageInput>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Trivia Facil", "Stage", "Trivia", 1, "test-token")
        });

    private static CreateSessionCommand MultiStageCommand(string name = "Multi Session")
        => new(name, new List<StageInput>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Trivia A", "Stage", "Trivia", 1, "test-token"),
            new(Guid.NewGuid(), Guid.NewGuid(), "Búsqueda Pirata", "Stage", "Treasure", 2, "test-token"),
            new(Guid.NewGuid(), Guid.NewGuid(), "Trivia C", "Stage", "Trivia", 3, "test-token")
        });

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateSessionWithSixDigitPin()
    {
        var command = SingleStageCommand();

        _repository.IsPinUniqueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        _repository.AddAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.Name.Should().Be("Test Session");
        result.Pin.Should().HaveLength(6);
        result.Pin.Should().MatchRegex(@"^\d{6}$");
        result.Status.Should().Be("Scheduled");
        result.CurrentStageOrder.Should().Be(0);
        result.Stages.Should().HaveCount(1);
        result.StartedAt.Should().BeNull();
        result.EndedAt.Should().BeNull();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        await _repository.Received(1).AddAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithMultiStageCommand_ShouldPreserveAllStagesWithOrder()
    {
        var command = MultiStageCommand();
        _repository.IsPinUniqueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _repository.AddAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Stages.Should().HaveCount(3);
        result.Stages[0].Order.Should().Be(1);
        result.Stages[1].Order.Should().Be(2);
        result.Stages[2].Order.Should().Be(3);
        result.Stages.Select(s => s.MissionType).Should().Contain(new[] { "Trivia", "Treasure", "Trivia" });
    }

    [Fact]
    public async Task Handle_WhenNameAlreadyExists_ShouldThrow()
    {
        var command = SingleStageCommand("Existing");

        // Build a session that matches the name
        var existing = Session.Create(
            "Existing",
            "999999",
            new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "M", "Stage", "Trivia", 1, "test-token") });
        _repository.GetByNameAsync("Existing", Arg.Any<CancellationToken>()).Returns(existing);

        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Ya existe una sesión*");
    }

    [Fact]
    public async Task Handle_WhenPinCollisionOccurs_ShouldRegeneratePinAndSucceed()
    {
        var command = SingleStageCommand("Collision");
        _repository.IsPinUniqueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false, false, false, false, false, false, false, false, false, true);
        _repository.AddAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Pin.Should().HaveLength(6);
        await _repository.Received(10).IsPinUniqueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPinCollisionExceedsMaxAttempts_ShouldThrow()
    {
        var command = SingleStageCommand("MaxCollision");
        _repository.IsPinUniqueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Could not generate unique PIN*");
        await _repository.DidNotReceive().AddAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldMapStageInputToSessionStage()
    {
        // Arrange
        var missionId1 = Guid.NewGuid();
        var missionId2 = Guid.NewGuid();
        var command = new CreateSessionCommand(
            "Mapping Test",
            new List<StageInput>
            {
                new(missionId1, Guid.NewGuid(), "Trivia Facil", "Stage", "Trivia", 1, "test-token"),
                new(missionId2, Guid.NewGuid(), "Busqueda", "Stage", "Treasure", 2, "test-token")
            });

        Session? captured = null;
        _repository.IsPinUniqueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _repository.AddAsync(Arg.Do<Session>(s => captured = s), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured!.Stages.Should().HaveCount(2);
        captured.Stages[0].MissionId.Should().Be(missionId1);
        captured.Stages[0].MissionTitle.Should().Be("Trivia Facil");
        captured.Stages[0].MissionType.Should().Be("Trivia");
        captured.Stages[1].MissionId.Should().Be(missionId2);
        captured.Stages[1].MissionType.Should().Be("Treasure");
    }
}


