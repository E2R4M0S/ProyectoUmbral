using FluentAssertions;
using Missions.Domain.ValueObjects;
using Xunit;

namespace Missions.Domain.Tests.ValueObjects;

public class EmailTests
{
    [Fact]
    public void Create_WithValidEmail_ShouldSucceed()
    {
        var email = Email.Create("Jugador@Example.com");

        ((string)email).Should().Be("jugador@example.com");
    }

    [Fact]
    public void Create_ShouldTrimWhitespace()
    {
        var email = Email.Create("  jugador@example.com  ");

        ((string)email).Should().Be("jugador@example.com");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithEmptyOrWhitespace_ShouldThrow(string? value)
    {
        var act = () => Email.Create(value!);

        act.Should().Throw<ArgumentException>().WithMessage("*empty*");
    }

    [Theory]
    [InlineData("noatsign.com")]
    [InlineData("no-dot@com")]
    [InlineData("justtext")]
    public void Create_WithInvalidFormat_ShouldThrow(string value)
    {
        var act = () => Email.Create(value);

        act.Should().Throw<ArgumentException>().WithMessage("*Invalid email format*");
    }

    [Fact]
    public void ImplicitStringConversion_ShouldReturnUnderlyingValue()
    {
        var email = Email.Create("jugador@example.com");

        string value = email;

        value.Should().Be("jugador@example.com");
    }
}
