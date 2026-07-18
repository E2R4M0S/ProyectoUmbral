using FluentAssertions;
using Sessions.Application.Sessions.Teams.CreateTeam;
using Xunit;

namespace Sessions.Application.Tests.Sessions.Teams.CreateTeam;

public class CreateSessionTeamCommandValidatorTests
{
    private readonly CreateSessionTeamCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithEmptySessionId_ShouldFail()
    {
        var command = new CreateSessionTeamCommand(Guid.Empty, "Los Ganadores");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.SessionId));
    }

    [Fact]
    public async Task Validate_WithEmptyName_ShouldFail()
    {
        var command = new CreateSessionTeamCommand(Guid.NewGuid(), "");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Name));
    }

    [Fact]
    public async Task Validate_WithNameOver100Chars_ShouldFail()
    {
        var command = new CreateSessionTeamCommand(Guid.NewGuid(), new string('A', 101));

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_WithValidCommand_ShouldPass()
    {
        var command = new CreateSessionTeamCommand(Guid.NewGuid(), "Los Ganadores");

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
