using FluentAssertions;
using Missions.Application.Operators.Disable;
using Xunit;

namespace Missions.Application.Tests.Operators.Disable;

public class DisableOperatorCommandValidatorTests
{
    private readonly DisableOperatorCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidEmail_ShouldPass()
    {
        var result = await _validator.ValidateAsync(new DisableOperatorCommand("carlos@umbral.local"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public async Task Validate_WithInvalidEmail_ShouldFail(string email)
    {
        var result = await _validator.ValidateAsync(new DisableOperatorCommand(email));

        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }
}
