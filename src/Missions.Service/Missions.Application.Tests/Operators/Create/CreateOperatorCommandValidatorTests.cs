using FluentAssertions;
using Missions.Application.Operators.Create;
using Xunit;

namespace Missions.Application.Tests.Operators.Create;

public class CreateOperatorCommandValidatorTests
{
    private readonly CreateOperatorCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidCommand_ShouldPass()
    {
        var result = await _validator.ValidateAsync(new CreateOperatorCommand("Carlos Mendoza", "carlos@umbral.local"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_WithEmptyName_ShouldFail()
    {
        var result = await _validator.ValidateAsync(new CreateOperatorCommand("", "carlos@umbral.local"));

        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public async Task Validate_WithNameOver100Chars_ShouldFail()
    {
        var result = await _validator.ValidateAsync(new CreateOperatorCommand(new string('A', 101), "carlos@umbral.local"));

        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public async Task Validate_WithInvalidEmail_ShouldFail(string email)
    {
        var result = await _validator.ValidateAsync(new CreateOperatorCommand("Carlos Mendoza", email));

        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }
}
