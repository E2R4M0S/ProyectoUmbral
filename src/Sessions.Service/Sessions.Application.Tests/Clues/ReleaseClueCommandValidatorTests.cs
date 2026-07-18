using FluentAssertions;
using Sessions.Application.Sessions.Clues;
using Xunit;

namespace Sessions.Application.Tests.Clues;

public class ReleaseClueCommandValidatorTests
{
    private readonly ReleaseClueCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithEmptySessionId_ShouldFail()
    {
        var command = new ReleaseClueCommand(Guid.Empty, Guid.NewGuid());

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.SessionId));
    }

    [Fact]
    public async Task Validate_WithEmptyClueId_ShouldFail()
    {
        var command = new ReleaseClueCommand(Guid.NewGuid(), Guid.Empty);

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.ClueId));
    }

    [Fact]
    public async Task Validate_WithValidCommand_ShouldPass()
    {
        var command = new ReleaseClueCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithValidCommandAndTeamId_ShouldPass()
    {
        var command = new ReleaseClueCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
