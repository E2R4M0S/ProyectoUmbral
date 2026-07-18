using FluentAssertions;
using Missions.Domain.ValueObjects;
using Xunit;

namespace Missions.Domain.Tests.ValueObjects;

public class AliasTests
{
    [Fact]
    public void Create_WithValidAlias_ShouldSucceed()
    {
        var alias = Alias.Create("jugador_1");

        ((string)alias).Should().Be("jugador_1");
    }

    [Fact]
    public void Create_WithLeadingAndTrailingWhitespace_ShouldTrim()
    {
        var alias = Alias.Create("  jugador1  ");

        ((string)alias).Should().Be("jugador1");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespace_ShouldThrow(string value)
    {
        var act = () => Alias.Create(value);

        act.Should().Throw<ArgumentException>().WithMessage("*empty*");
    }

    [Fact]
    public void Create_WithTooShortAlias_ShouldThrow()
    {
        var act = () => Alias.Create("ab");

        act.Should().Throw<ArgumentException>().WithMessage("*between 3 and 50*");
    }

    [Fact]
    public void Create_WithTooLongAlias_ShouldThrow()
    {
        var act = () => Alias.Create(new string('a', 51));

        act.Should().Throw<ArgumentException>().WithMessage("*between 3 and 50*");
    }

    [Theory]
    [InlineData("juan.perez")]
    [InlineData("juan-perez")]
    [InlineData("juan perez")]
    [InlineData("juan@perez")]
    public void Create_WithNonAlphanumericCharacters_ShouldThrow(string value)
    {
        var act = () => Alias.Create(value);

        act.Should().Throw<ArgumentException>().WithMessage("*alphanumeric*");
    }

    [Fact]
    public void ImplicitStringConversion_ShouldReturnUnderlyingValue()
    {
        var alias = Alias.Create("jugador1");

        string value = alias;

        value.Should().Be("jugador1");
    }
}
