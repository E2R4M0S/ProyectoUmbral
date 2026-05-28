using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Teams.Application.Common.Interfaces;
using Teams.Application.Teams.Join;
using Teams.Domain.Entities;
using Xunit;

namespace Teams.Application.Tests.Teams.Join;

public class JoinTeamCommandHandlerTests
{
    private readonly ITeamRepository _repository = Substitute.For<ITeamRepository>();
    private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
    private readonly ILogger<JoinTeamCommandHandler> _logger =
        Substitute.For<ILogger<JoinTeamCommandHandler>>();
    private readonly JoinTeamCommandHandler _sut;

    public JoinTeamCommandHandlerTests()
    {
        _sut = new JoinTeamCommandHandler(_repository, _httpContextAccessor, _logger);
    }

    private static Team CreateTeamWithJoinCode(string joinCode)
    {
        var team = Team.Create("Test Team", "Test Description", "leader-123");
        // Use reflection to set the JoinCode since it has a private setter
        typeof(Team).GetProperty("JoinCode")!.SetValue(team, joinCode);
        return team;
    }

    private void SetupUserContext(string userId)
    {
        var claims = new List<Claim>
        {
            new Claim("sub", userId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal
        };
        _httpContextAccessor.HttpContext.Returns(httpContext);
    }

    [Fact]
    public async Task Handle_HappyPath_ShouldJoinTeamSuccessfully()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = "user-456";
        var joinCode = "ABC123";
        var command = new JoinTeamCommand(teamId, joinCode);

        var team = CreateTeamWithJoinCode(joinCode);
        // Use reflection to set the team Id since it's private set
        typeof(Team).GetProperty("Id")!.SetValue(team, teamId);

        SetupUserContext(userId);

        _repository.GetByIdWithMembersAsync(teamId, Arg.Any<CancellationToken>())
            .Returns(team);
        _repository.UpdateAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.TeamId.Should().Be(teamId);

        await _repository.Received(1).GetByIdWithMembersAsync(teamId, Arg.Any<CancellationToken>());
        await _repository.Received(1).UpdateAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TeamNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var joinCode = "ABC123";
        var command = new JoinTeamCommand(teamId, joinCode);

        SetupUserContext("user-456");

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
    public async Task Handle_InvalidJoinCode_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var teamJoinCode = "ABC123";
        var wrongJoinCode = "WRONG1";
        var command = new JoinTeamCommand(teamId, wrongJoinCode);

        var team = CreateTeamWithJoinCode(teamJoinCode);
        typeof(Team).GetProperty("Id")!.SetValue(team, teamId);

        SetupUserContext("user-456");

        _repository.GetByIdWithMembersAsync(teamId, Arg.Any<CancellationToken>())
            .Returns(team);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*invalid*join*code*");

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyMember_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var teamId = Guid.NewGuid();
        var userId = "user-456";
        var joinCode = "ABC123";
        var command = new JoinTeamCommand(teamId, joinCode);

        var team = CreateTeamWithJoinCode(joinCode);
        typeof(Team).GetProperty("Id")!.SetValue(team, teamId);
        team.AddMember(userId); // User is already a member

        SetupUserContext(userId);

        _repository.GetByIdWithMembersAsync(teamId, Arg.Any<CancellationToken>())
            .Returns(team);

        // Act
        Func<Task> act = async () => await _sut.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already*a*member*");

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Team>(), Arg.Any<CancellationToken>());
    }
}
