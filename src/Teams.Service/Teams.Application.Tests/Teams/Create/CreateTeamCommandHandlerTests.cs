using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Teams.Application.Common.Interfaces;
using Teams.Application.Teams.Create;
using Teams.Application.Teams.Users.GetUsers;
using Teams.Domain.Entities;
using Xunit;

namespace Teams.Application.Tests.Teams.Create;

public class CreateTeamCommandHandlerTests
{
    private readonly ITeamRepository _repository = Substitute.For<ITeamRepository>();
    private readonly IKeycloakAdminService _keycloakAdminService = Substitute.For<IKeycloakAdminService>();
    private readonly ILogger<CreateTeamCommandHandler> _logger =
        Substitute.For<ILogger<CreateTeamCommandHandler>>();
    private readonly CreateTeamCommandHandler _sut;

    public CreateTeamCommandHandlerTests()
    {
        _sut = new CreateTeamCommandHandler(_repository, _keycloakAdminService, _logger);
    }

    [Fact]
    public async Task Handle_HappyPath_ShouldCreateTeamWithMembers()
    {
        // Arrange
        var command = new CreateTeamCommand(
            Name: "Los Leones",
            Description: "Equipo de desarrollo",
            LeaderId: "leader-123",
            MemberIds: new List<string> { "user-1", "user-2" });

        _repository.IsNameUniqueAsync("Los Leones", Arg.Any<CancellationToken>()).Returns(true);
        _keycloakAdminService.GetUserByIdAsync("leader-123", Arg.Any<CancellationToken>())
            .Returns(new UserRepresentation { Id = "leader-123" });
        _keycloakAdminService.GetUserByIdAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(new UserRepresentation { Id = "user-1" });
        _keycloakAdminService.GetUserByIdAsync("user-2", Arg.Any<CancellationToken>())
            .Returns(new UserRepresentation { Id = "user-2" });

        _repository.AddAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Los Leones");
        result.Description.Should().Be("Equipo de desarrollo");
        result.LeaderId.Should().Be("leader-123");
        result.MemberIds.Should().HaveCount(3); // leader + 2 members
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        await _repository.Received(1).AddAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateName_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var command = new CreateTeamCommand(
            Name: "Los Leones",
            Description: "Equipo de desarrollo",
            LeaderId: "leader-123",
            MemberIds: new List<string>());

        _repository.IsNameUniqueAsync("Los Leones", Arg.Any<CancellationToken>()).Returns(false);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");

        await _repository.DidNotReceive().AddAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LeaderNotExists_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var command = new CreateTeamCommand(
            Name: "Los Tigres",
            Description: "Equipo QA",
            LeaderId: "unknown-leader",
            MemberIds: new List<string>());

        _repository.IsNameUniqueAsync("Los Tigres", Arg.Any<CancellationToken>()).Returns(true);
        _keycloakAdminService.GetUserByIdAsync("unknown-leader", Arg.Any<CancellationToken>())
            .Returns((UserRepresentation?)null);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Leader*not found*");

        await _repository.DidNotReceive().AddAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MemberNotExists_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var command = new CreateTeamCommand(
            Name: "Los Halcones",
            Description: "Equipo frontend",
            LeaderId: "leader-123",
            MemberIds: new List<string> { "user-1", "unknown-user" });

        _repository.IsNameUniqueAsync("Los Halcones", Arg.Any<CancellationToken>()).Returns(true);
        _keycloakAdminService.GetUserByIdAsync("leader-123", Arg.Any<CancellationToken>())
            .Returns(new UserRepresentation { Id = "leader-123" });
        _keycloakAdminService.GetUserByIdAsync("user-1", Arg.Any<CancellationToken>())
            .Returns(new UserRepresentation { Id = "user-1" });
        _keycloakAdminService.GetUserByIdAsync("unknown-user", Arg.Any<CancellationToken>())
            .Returns((UserRepresentation?)null);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Member*not found*");

        await _repository.DidNotReceive().AddAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoMembers_ShouldCreateTeamWithEmptyMemberList()
    {
        // Arrange
        var command = new CreateTeamCommand(
            Name: "Los Búhos",
            Description: "Equipo backend",
            LeaderId: "leader-789",
            MemberIds: new List<string>());

        _repository.IsNameUniqueAsync("Los Búhos", Arg.Any<CancellationToken>()).Returns(true);
        _keycloakAdminService.GetUserByIdAsync("leader-789", Arg.Any<CancellationToken>())
            .Returns(new UserRepresentation { Id = "leader-789" });

        _repository.AddAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.MemberIds.Should().HaveCount(1); // leader is always added as member
        await _repository.Received(1).AddAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_HappyPath_ShouldGenerateJoinCode()
    {
        // Arrange
        var command = new CreateTeamCommand(
            Name: "Los Lobos",
            Description: "Equipo móvil",
            LeaderId: "leader-999",
            MemberIds: new List<string>());

        _repository.IsNameUniqueAsync("Los Lobos", Arg.Any<CancellationToken>()).Returns(true);
        _keycloakAdminService.GetUserByIdAsync("leader-999", Arg.Any<CancellationToken>())
            .Returns(new UserRepresentation { Id = "leader-999" });

        Team? capturedTeam = null;
        _repository.AddAsync(Arg.Do<Team>(t => capturedTeam = t), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        capturedTeam.Should().NotBeNull();
        capturedTeam!.JoinCode.Should().NotBeNullOrEmpty();
        capturedTeam.JoinCode.Should().HaveLength(6);
    }
}
