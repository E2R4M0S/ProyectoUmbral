using FluentAssertions;
using Missions.Application.Participants.Register;
using Xunit;

namespace Missions.Application.Tests.Participants.Register;

public class RegisterParticipantCommandValidatorTests
{
    private readonly RegisterParticipantCommandValidator _validator = new();

    private static RegisterParticipantCommand ValidCommand() => new(
        "Juan", "Perez", "juanperez", "jugador1", "juan@example.com", "Password123");

    [Fact]
    public async Task Validate_WithValidCommand_ShouldPass()
    {
        var result = await _validator.ValidateAsync(ValidCommand());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyFirstName_ShouldFail()
    {
        var command = ValidCommand() with { FirstName = "" };

        var result = await _validator.ValidateAsync(command);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.FirstName));
    }

    [Fact]
    public async Task Validate_WithFirstNameOver50Chars_ShouldFail()
    {
        var command = ValidCommand() with { FirstName = new string('A', 51) };

        var result = await _validator.ValidateAsync(command);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.FirstName));
    }

    [Fact]
    public async Task Validate_WithEmptyLastName_ShouldFail()
    {
        var command = ValidCommand() with { LastName = "" };

        var result = await _validator.ValidateAsync(command);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.LastName));
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("juan.perez")]
    [InlineData("juan perez")]
    public async Task Validate_WithInvalidUsername_ShouldFail(string username)
    {
        var command = ValidCommand() with { Username = username };

        var result = await _validator.ValidateAsync(command);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Username));
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("jugador.1")]
    [InlineData("jugador 1")]
    public async Task Validate_WithInvalidAlias_ShouldFail(string alias)
    {
        var command = ValidCommand() with { Alias = alias };

        var result = await _validator.ValidateAsync(command);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Alias));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public async Task Validate_WithInvalidEmail_ShouldFail(string email)
    {
        var command = ValidCommand() with { Email = email };

        var result = await _validator.ValidateAsync(command);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Email));
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    public async Task Validate_WithInvalidPassword_ShouldFail(string password)
    {
        var command = ValidCommand() with { Password = password };

        var result = await _validator.ValidateAsync(command);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Password));
    }
}
