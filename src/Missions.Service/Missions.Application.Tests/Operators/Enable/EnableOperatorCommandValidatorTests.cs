using FluentAssertions;
using Missions.Application.Operators.Enable;
using Xunit;

namespace Missions.Application.Tests.Operators.Enable;

public class EnableOperatorCommandValidatorTests
{
    private readonly EnableOperatorCommandValidator _validator = new();

    [Fact]
    public async Task Validate_WithValidEmail_ShouldPass()
    {
        var result = await _validator.ValidateAsync(new EnableOperatorCommand("carlos@umbral.local"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public async Task Validate_WithInvalidEmail_ShouldFail(string email)
    {
        var result = await _validator.ValidateAsync(new EnableOperatorCommand(email));

        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }
}
