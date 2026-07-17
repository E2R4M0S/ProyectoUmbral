using System.Security.Claims;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
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
    private readonly IMissionCatalogService _missionCatalogService = Substitute.For<IMissionCatalogService>();
    private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
    private readonly ILogger<CreateSessionCommandHandler> _logger =
        Substitute.For<ILogger<CreateSessionCommandHandler>>();
    private readonly CreateSessionCommandHandler _sut;
    private readonly Guid _operatorId = Guid.NewGuid();

    public CreateSessionCommandHandlerTests()
    {
        _missionCatalogService
            .GetMissionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => new MissionSummary(ci.Arg<Guid>(), "Some Mission", "Active"));

        var identity = new ClaimsIdentity(new[] { new Claim("sub", _operatorId.ToString()) }, "Test");
        _httpContextAccessor.HttpContext.Returns(new DefaultHttpContext { User = new ClaimsPrincipal(identity) });

        _sut = new CreateSessionCommandHandler(_repository, _missionCatalogService, _httpContextAccessor, _logger);
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
    public async Task Handle_WithValidCommand_ShouldPropagateMissionDifficultyToStage()
    {
        var command = SingleStageCommand();
        var missionId = command.Stages[0].MissionId;
        _missionCatalogService
            .GetMissionAsync(missionId, Arg.Any<CancellationToken>())
            .Returns(new MissionSummary(missionId, "Some Mission", "Active", "Hard"));

        _repository.IsPinUniqueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        _repository.AddAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await _sut.Handle(command, CancellationToken.None);

        await _repository.Received(1).AddAsync(
            Arg.Is<Session>(s => s.Stages.First().Difficulty == "Hard" && s.Stages.First().BaseScanPoints == 200),
            Arg.Any<CancellationToken>());
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

    [Fact]
    public async Task Handle_WhenMissionIsNotActive_ShouldThrowValidationException()
    {
        var command = SingleStageCommand("Draft Mission Session");
        _missionCatalogService
            .GetMissionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(ci => new MissionSummary(ci.Arg<Guid>(), "Trivia Facil", "Draft"));

        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*no está Activa*");
        await _repository.DidNotReceive().AddAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenMissionDoesNotExist_ShouldThrowValidationException()
    {
        var command = SingleStageCommand("Missing Mission Session");
        _missionCatalogService
            .GetMissionAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((MissionSummary?)null);

        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*no existe*");
        await _repository.DidNotReceive().AddAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldSetOperatorIdFromToken()
    {
        var command = SingleStageCommand("Owned Session");
        _repository.IsPinUniqueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        Session? captured = null;
        _repository.AddAsync(Arg.Do<Session>(s => captured = s), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await _sut.Handle(command, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.OperatorId.Should().Be(_operatorId);
    }

    [Fact]
    public async Task Handle_WhenNoOperatorClaimInToken_ShouldThrow()
    {
        _httpContextAccessor.HttpContext.Returns(new DefaultHttpContext());
        var command = SingleStageCommand("No Operator Session");

        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Operator identifier*");
        await _repository.DidNotReceive().AddAsync(Arg.Any<Session>(), Arg.Any<CancellationToken>());
    }
}


