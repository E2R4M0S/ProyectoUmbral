using System.Security.Claims;
using FluentAssertions;
using Xunit;

namespace ApiGateway.Tests;

public class KeycloakRolesTransformerTests
{
    private readonly KeycloakRolesTransformer _sut = new();

    [Fact]
    public async Task TransformAsync_WithNoRealmAccess_ReturnsOriginalPrincipal()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await _sut.TransformAsync(principal);

        result.Should().BeSameAs(principal);
    }

    [Fact]
    public async Task TransformAsync_WithValidRoles_AddsRoleClaims()
    {
        var json = """{"roles":["admin","operator"]}""";
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim("realm_access", json));
        var principal = new ClaimsPrincipal(identity);

        var result = await _sut.TransformAsync(principal);

        result.IsInRole("admin").Should().BeTrue();
        result.IsInRole("operator").Should().BeTrue();
        result.IsInRole("participant").Should().BeFalse();
    }

    [Fact]
    public async Task TransformAsync_WithEmptyRoles_ReturnsOriginalPrincipal()
    {
        var json = """{"roles":[]}""";
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim("realm_access", json));
        var principal = new ClaimsPrincipal(identity);

        var result = await _sut.TransformAsync(principal);

        result.Should().BeSameAs(principal);
    }

    [Fact]
    public async Task TransformAsync_WithInvalidJson_ReturnsOriginalPrincipal()
    {
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim("realm_access", "not-json"));
        var principal = new ClaimsPrincipal(identity);

        var result = await _sut.TransformAsync(principal);

        result.Should().BeSameAs(principal);
    }

    [Fact]
    public async Task TransformAsync_WithNoRolesProperty_ReturnsOriginalPrincipal()
    {
        var json = """{"other":"value"}""";
        var identity = new ClaimsIdentity();
        identity.AddClaim(new Claim("realm_access", json));
        var principal = new ClaimsPrincipal(identity);

        var result = await _sut.TransformAsync(principal);

        result.Should().BeSameAs(principal);
    }
}
