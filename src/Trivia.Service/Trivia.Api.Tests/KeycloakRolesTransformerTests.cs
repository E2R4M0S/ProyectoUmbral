using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Trivia.Api.Tests;

public class KeycloakRolesTransformerTests
{
    private static ClaimsPrincipal CreatePrincipalWithRealmAccess(string[] roles)
    {
        var realmAccess = JsonSerializer.Serialize(new { roles });
        var identity = new ClaimsIdentity(new[]
        {
            new Claim("realm_access", realmAccess)
        }, "Test");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public async Task TransformAsync_WithAdminRole_AddsAdminClaim()
    {
        var principal = CreatePrincipalWithRealmAccess(new[] { "admin", "offline_access" });
        var transformer = new Trivia.Api.KeycloakRolesTransformer();

        var result = await transformer.TransformAsync(principal);

        result.IsInRole("admin").Should().BeTrue();
        result.IsInRole("operator").Should().BeFalse();
    }

    [Fact]
    public async Task TransformAsync_WithOperatorRole_AddsOperatorClaim()
    {
        var principal = CreatePrincipalWithRealmAccess(new[] { "operator" });
        var transformer = new Trivia.Api.KeycloakRolesTransformer();

        var result = await transformer.TransformAsync(principal);

        result.IsInRole("operator").Should().BeTrue();
        result.IsInRole("admin").Should().BeFalse();
    }

    [Fact]
    public async Task TransformAsync_WithoutRealmAccess_ReturnsSamePrincipal()
    {
        var identity = new ClaimsIdentity(new[] { new Claim("sub", "user123") }, "Test");
        var principal = new ClaimsPrincipal(identity);
        var transformer = new Trivia.Api.KeycloakRolesTransformer();

        var result = await transformer.TransformAsync(principal);

        result.IsInRole("admin").Should().BeFalse();
        result.IsInRole("operator").Should().BeFalse();
    }

    [Fact]
    public async Task TransformAsync_WithParticipantRole_AddsParticipantClaim()
    {
        var principal = CreatePrincipalWithRealmAccess(new[] { "participant" });
        var transformer = new Trivia.Api.KeycloakRolesTransformer();

        var result = await transformer.TransformAsync(principal);

        result.IsInRole("participant").Should().BeTrue();
    }
}
