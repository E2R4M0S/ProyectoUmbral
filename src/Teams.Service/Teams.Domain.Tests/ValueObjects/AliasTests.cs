using FluentAssertions;
using Teams.Domain.ValueObjects;
using Xunit;

namespace Teams.Domain.Tests.ValueObjects;

public class AliasTests
{
    [Fact]
    public void Create_ValidAlias_ShouldReturnAlias()
    {
        var alias = Alias.Create("john_doe");

        alias.Value.Should().Be("john_doe");
    }

    [Fact]
    public void Create_Null_ShouldThrow()
    {
        Action act = () => Alias.Create(null!);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Alias*");
    }

    [Fact]
    public void Create_Empty_ShouldThrow()
    {
        Action act = () => Alias.Create("");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Alias*");
    }

    [Fact]
    public void Create_Whitespace_ShouldThrow()
    {
        Action act = () => Alias.Create("   ");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Alias*");
    }

    [Fact]
    public void Create_TooShort_ShouldThrow()
    {
        Action act = () => Alias.Create("ab");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*3 and 50*");
    }

    [Fact]
    public void Create_TooLong_ShouldThrow()
    {
        var longAlias = new string('a', 51);

        Action act = () => Alias.Create(longAlias);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*3 and 50*");
    }

    [Fact]
    public void Create_InvalidChars_ShouldThrow()
    {
        Action act = () => Alias.Create("john-doe");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*alphanumeric*");
    }

    [Fact]
    public void Create_BoundaryMin_ShouldSucceed()
    {
        var alias = Alias.Create("abc");

        alias.Value.Should().Be("abc");
    }

    [Fact]
    public void Create_BoundaryMax_ShouldSucceed()
    {
        var maxAlias = new string('a', 50);

        var alias = Alias.Create(maxAlias);

        alias.Value.Should().Be(maxAlias);
    }

    [Fact]
    public void Create_TrimsWhitespace()
    {
        var alias = Alias.Create("john_doe  ");

        alias.Value.Should().Be("john_doe");
    }
}
