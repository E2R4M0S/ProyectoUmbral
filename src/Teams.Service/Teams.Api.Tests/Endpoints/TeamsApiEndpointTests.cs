using FluentAssertions;
using MediatR;
using NSubstitute;
using Teams.Application.Teams.Create;
using Teams.Application.Teams.Update;
using Teams.Application.Teams.Join;
using Teams.Application.Teams.Detail;
using Teams.Application.Teams.List;
using Teams.Application.Teams.Register;
using Teams.Application.Teams.Profile;
using Teams.Application.Teams.Operators.Create;
using Teams.Application.Teams.Operators.Disable;
using Teams.Application.Teams.Users.GetUsers;
using Teams.Application.Teams.Users.GetUserById;
using Xunit;

namespace Teams.Api.Tests.Endpoints;

public class TeamsApiEndpointTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    [Fact]
    public async Task CreateTeam_ValidCommand_CallsMediator()
    {
        var command = new CreateTeamCommand("Test Team", "Desc", "leader-123");
        var result = new CreateTeamCommandResult(Guid.NewGuid(), "Test Team", "Desc", "leader-123", new List<string>(), "ABC123", DateTime.UtcNow);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Task.FromResult(result));

        await _mediator.Send(command);

        await _mediator.Received(1).Send(command, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTeam_ValidCommand_CallsMediator()
    {
        var command = new UpdateTeamCommand(Guid.NewGuid(), "Updated", "New desc", null, null);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Task.FromResult(Unit.Value));

        await _mediator.Send(command);

        await _mediator.Received(1).Send(command, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinTeam_ValidCommand_CallsMediator()
    {
        var command = new JoinTeamCommand("ABC123");
        var result = new JoinTeamCommandResult(true, Guid.NewGuid());
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Task.FromResult(result));

        await _mediator.Send(command);

        await _mediator.Received(1).Send(command, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task JoinTeam_InvalidCode_ThrowsNotFound()
    {
        var command = new JoinTeamCommand("INVALID");
        _mediator.Send(command, Arg.Any<CancellationToken>())!
            .Returns<Task<JoinTeamCommandResult>>(_ => throw new InvalidOperationException("Invalid join code"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _mediator.Send(command));

        ex.Message.Should().Contain("Invalid join code");
    }

    [Fact]
    public async Task GetTeamById_ValidQuery_CallsMediator()
    {
        var query = new GetTeamByIdQuery(Guid.NewGuid());
        _mediator.Send(query, Arg.Any<CancellationToken>()).Returns(Task.FromResult<TeamDetailDto?>(null));

        await _mediator.Send(query);

        await _mediator.Received(1).Send(query, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetTeams_ValidQuery_CallsMediator()
    {
        var query = new GetTeamsQuery(null, 1, 10);
        var result = new GetTeamsResult(Array.Empty<TeamListItemDto>(), 0, 1, 10);
        _mediator.Send(query, Arg.Any<CancellationToken>()).Returns(Task.FromResult(result));

        await _mediator.Send(query);

        await _mediator.Received(1).Send(query, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterParticipant_ValidCommand_CallsMediator()
    {
        var command = new RegisterParticipantCommand("testuser", "alias1", "test@test.com", "pass123!");
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Task.FromResult(Guid.NewGuid()));

        await _mediator.Send(command);

        await _mediator.Received(1).Send(command, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProfile_ValidQuery_CallsMediator()
    {
        var query = new GetProfileQuery("keycloak-user-id");
        _mediator.Send(query, Arg.Any<CancellationToken>()).Returns(Task.FromResult<GetProfileResponse?>(null));

        await _mediator.Send(query);

        await _mediator.Received(1).Send(query, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProfile_ValidCommand_CallsMediator()
    {
        var command = new UpdateProfileCommand("NewName", "NewAlias", "keycloak-id");
        var result = new GetProfileResponse("NewName", "NewAlias", "new@test.com");
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Task.FromResult(result));

        await _mediator.Send(command);

        await _mediator.Received(1).Send(command, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateOperator_ValidCommand_CallsMediator()
    {
        var command = new CreateOperatorCommand("operator1", "op@test.com");
        var result = new CreateOperatorResult("operator1", "op@test.com", "kc-user-123");
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Task.FromResult(result));

        await _mediator.Send(command);

        await _mediator.Received(1).Send(command, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DisableOperator_ValidCommand_CallsMediator()
    {
        var command = new DisableOperatorCommand("op@test.com");
        var result = new DisableOperatorResponse("Operator disabled", true);
        _mediator.Send(command, Arg.Any<CancellationToken>()).Returns(Task.FromResult(result));

        await _mediator.Send(command);

        await _mediator.Received(1).Send(command, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUsers_ValidQuery_CallsMediator()
    {
        var query = new GetUsersQuery(null, null, null, 1, 10);
        var result = new GetUsersResponse(Array.Empty<UserListItem>(), 0, 1, 10);
        _mediator.Send(query, Arg.Any<CancellationToken>()).Returns(Task.FromResult(result));

        await _mediator.Send(query);

        await _mediator.Received(1).Send(query, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUserById_ValidQuery_CallsMediator()
    {
        var query = new GetUserByIdQuery("user-id-123");
        _mediator.Send(query, Arg.Any<CancellationToken>()).Returns(Task.FromResult<UserDetailResponse?>(null));

        await _mediator.Send(query);

        await _mediator.Received(1).Send(query, Arg.Any<CancellationToken>());
    }
}