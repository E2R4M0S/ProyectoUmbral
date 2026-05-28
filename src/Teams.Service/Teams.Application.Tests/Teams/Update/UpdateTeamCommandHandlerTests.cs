using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Teams.Application.Common.Interfaces;
using Teams.Application.Teams.Update;
using Teams.Application.Teams.Users.GetUsers;
using Teams.Domain.Entities;
using Xunit;

namespace Teams.Application.Tests.Teams.Update;

public class UpdateTeamCommandHandlerTests
{
    private readonly ITeamRepository _repository = Substitute.For<ITeamRepository>();
    private readonly IKeycloakAdminService _keycloakAdminService = Substitute.For<IKeycloakAdminService>();
    private readonly ILogger<UpdateTeamCommandHandler> _logger =
        Substitute.For<ILogger<UpdateTeamCommandHandler>>();
    private readonly UpdateTeamCommandHandler _sut;

    public UpdateTeamCommandHandlerTests()
    {
        _sut = new UpdateTeamCommandHandler(_repository, _keycloakAdminService, _logger);
    }

    private static Team CreateTeamWithId(Guid id)
    {
        var team = Team.Create("Original Team", "Original Description", "leader-123");
        return team;
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldUpdateTeamNameAndDescription()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var command = new UpdateTeamCommand(
            teamId,
            "Updated Team",
            "Updated Description",
            null,
            null);

        var existingTeam = CreateTeamWithId(teamId);

        _repository.GetByIdWithMembersAsync(teamId, Arg.Any<CancellationToken>())
            .Returns(existingTeam);
        _repository.UpdateAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).GetByIdWithMembersAsync(teamId, Arg.Any<CancellationToken>());
        await _repository.Received(1).UpdateAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TeamNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var command = new UpdateTeamCommand(
            teamId,
            "Updated Team",
            "Updated Description",
            null,
            null);

        _repository.GetByIdWithMembersAsync(teamId, Arg.Any<CancellationToken>())
            .Returns((Team?)null);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AddMembers_ShouldAddMembersToTeam()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var command = new UpdateTeamCommand(
            teamId,
            "Updated Team",
            "Updated Description",
            new List<string> { "user-1", "user-2" },
            null);

        var existingTeam = CreateTeamWithId(teamId);

        _repository.GetByIdWithMembersAsync(teamId, Arg.Any<CancellationToken>())
            .Returns(existingTeam);
        _keycloakAdminService.GetUserByIdAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(new UserRepresentation { Id = "user-1" });
        _keycloakAdminService.GetUserByIdAsync("user-2", Arg.Any<CancellationToken>())
            .Returns(new UserRepresentation { Id = "user-2" });
        _repository.UpdateAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _keycloakAdminService.Received(1).GetUserByIdAsync("user-1", Arg.Any<CancellationToken>());
        await _keycloakAdminService.Received(1).GetUserByIdAsync("user-2", Arg.Any<CancellationToken>());
        await _repository.Received(1).UpdateAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RemoveMembers_ShouldRemoveMembersFromTeam()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var command = new UpdateTeamCommand(
            teamId,
            "Updated Team",
            "Updated Description",
            null,
            new List<string> { "user-1" });

        var existingTeam = CreateTeamWithId(teamId);
        existingTeam.AddMember("user-1");

        _repository.GetByIdWithMembersAsync(teamId, Arg.Any<CancellationToken>())
            .Returns(existingTeam);
        _repository.UpdateAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(command, CancellationToken.None);

        // Assert
        await _repository.Received(1).UpdateAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AddMemberUserNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var command = new UpdateTeamCommand(
            teamId,
            "Updated Team",
            "Updated Description",
            new List<string> { "unknown-user" },
            null);

        var existingTeam = CreateTeamWithId(teamId);

        _repository.GetByIdWithMembersAsync(teamId, Arg.Any<CancellationToken>())
            .Returns(existingTeam);
        _keycloakAdminService.GetUserByIdAsync("unknown-user", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<UserRepresentation?>(null));

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RemoveMemberNotInTeam_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var command = new UpdateTeamCommand(
            teamId,
            "Updated Team",
            "Updated Description",
            null,
            new List<string> { "not-a-member" });

        var existingTeam = CreateTeamWithId(teamId);

        _repository.GetByIdWithMembersAsync(teamId, Arg.Any<CancellationToken>())
            .Returns(existingTeam);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not a member*");

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AddDuplicateMember_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var command = new UpdateTeamCommand(
            teamId,
            "Updated Team",
            "Updated Description",
            new List<string> { "user-1" },
            null);

        var existingTeam = CreateTeamWithId(teamId);
        existingTeam.AddMember("user-1");

        _repository.GetByIdWithMembersAsync(teamId, Arg.Any<CancellationToken>())
            .Returns(existingTeam);
        _keycloakAdminService.GetUserByIdAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(new UserRepresentation { Id = "user-1" });

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already a member*");

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>());
    }
}
