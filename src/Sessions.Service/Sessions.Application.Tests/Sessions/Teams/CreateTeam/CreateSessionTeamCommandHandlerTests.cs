using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.Teams.CreateTeam;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;
using Xunit;

namespace Sessions.Application.Tests.Sessions.Teams.CreateTeam;

public class CreateSessionTeamCommandHandlerTests
{
    private readonly ISessionRepository _repository = Substitute.For<ISessionRepository>();
    private readonly ILogger<CreateSessionTeamCommandHandler> _logger =
        Substitute.For<ILogger<CreateSessionTeamCommandHandler>>();
    private readonly CreateSessionTeamCommandHandler _sut;

    public CreateSessionTeamCommandHandlerTests()
    {
        _sut = new CreateSessionTeamCommandHandler(_repository, _logger);
    }

    [Fact]
    public async Task Handle_WhenSessionNotFound_ShouldThrow()
    {
        var command = new CreateSessionTeamCommand(Guid.NewGuid(), "Los Ganadores");
        _repository.GetByIdAsync(command.SessionId, Arg.Any<CancellationToken>()).Returns((Session?)null);

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no encontrada*");
    }

    [Theory]
    [InlineData(SessionStatus.Active)]
    [InlineData(SessionStatus.Finished)]
    [InlineData(SessionStatus.Cancelled)]
    public async Task Handle_WhenSessionNotInPreparingOrScheduled_ShouldThrow(SessionStatus status)
    {
        var session = CreateSessionWithStatus(status);
        var command = new CreateSessionTeamCommand(session.Id, "Los Ganadores");
        _repository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Programada o En Preparación*");
    }

    [Fact]
    public async Task Handle_WhenNameNotUnique_ShouldThrow()
    {
        var session = CreateSessionWithStatus(SessionStatus.Preparing);
        var command = new CreateSessionTeamCommand(session.Id, "Duplicado");
        _repository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _repository.IsTeamNameUniqueInSessionAsync(session.Id, "Duplicado", Arg.Any<CancellationToken>())
            .Returns(false);

        var act = async () => await _sut.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Ya existe un equipo*");
    }

    [Theory]
    [InlineData(SessionStatus.Preparing)]
    [InlineData(SessionStatus.Scheduled)]
    public async Task Handle_WithValidCommand_ShouldCreateAndPersistTeam(SessionStatus status)
    {
        var session = CreateSessionWithStatus(status);
        var command = new CreateSessionTeamCommand(session.Id, "Los Ganadores");
        _repository.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _repository.IsTeamNameUniqueInSessionAsync(session.Id, "Los Ganadores", Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.Name.Should().Be("Los Ganadores");
        result.MaxMembers.Should().Be(5);
        result.TeamId.Should().NotBeEmpty();
        await _repository.Received(1).AddTeamAsync(
            Arg.Is<SessionTeam>(t => t.Name == "Los Ganadores" && t.SessionId == session.Id),
            Arg.Any<CancellationToken>());
    }

    private static Session CreateSessionWithStatus(SessionStatus status)
    {
        var session = Session.Create("Test Session", "123456",
            new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Test Mission", "Stage", "Trivia", 1, "test-token") });
        var statusProperty = typeof(Session).GetProperty("Status")!;
        statusProperty.SetValue(session, status);
        return session;
    }
}
