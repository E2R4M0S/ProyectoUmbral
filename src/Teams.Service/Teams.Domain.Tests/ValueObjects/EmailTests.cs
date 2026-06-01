using FluentAssertions;
using Teams.Domain.ValueObjects;
using Xunit;

namespace Teams.Domain.Tests.ValueObjects;

public class EmailTests
{
    [Fact]
    public void Create_ValidEmail_ShouldReturnEmail()
    {
        var email = Email.Create("test@example.com");

        email.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void Create_Null_ShouldThrow()
    {
        Action act = () => Email.Create(null!);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Email*");
    }

    [Fact]
    public void Create_Empty_ShouldThrow()
    {
        Action act = () => Email.Create("");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Email*");
    }

    [Fact]
    public void Create_Whitespace_ShouldThrow()
    {
        Action act = () => Email.Create("   ");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Email*");
    }

    [Fact]
    public void Create_NoAtSymbol_ShouldThrow()
    {
        Action act = () => Email.Create("testexample.com");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid email*");
    }

    [Fact]
    public void Create_NoDot_ShouldThrow()
    {
        Action act = () => Email.Create("test@example");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid email*");
    }

    [Fact]
    public void Create_TrimsWhitespace()
    {
        var email = Email.Create("  test@example.com  ");

        email.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void Create_NormalizesToLower()
    {
        var email = Email.Create("TEST@EXAMPLE.COM");

        email.Value.Should().Be("test@example.com");
    }
}
