using FluentAssertions;
using Missions.Application.Participants.Profile;
using Xunit;

namespace Missions.Application.Tests.Participants.Profile;

public class UpdateProfileCommandValidatorTests
{
    private readonly UpdateProfileCommandValidator _validator = new();

    private static UpdateProfileCommand ValidCommand() => new("Juan", "Perez", "jugador1", "kc-1");

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
    [InlineData("jugador.1")]
    [InlineData("jugador 1")]
    public async Task Validate_WithInvalidAlias_ShouldFail(string alias)
    {
        var command = ValidCommand() with { Alias = alias };

        var result = await _validator.ValidateAsync(command);

        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Alias));
    }
}
