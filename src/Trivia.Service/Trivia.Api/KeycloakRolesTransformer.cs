using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace Trivia.Api;

public sealed class KeycloakRolesTransformer : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var realmAccess = principal.FindFirst("realm_access")?.Value;
        if (realmAccess is null)
            return Task.FromResult(principal);

        JsonElement root;
        try
        {
            root = JsonSerializer.Deserialize<JsonElement>(realmAccess);
        }
        catch (JsonException)
        {
            return Task.FromResult(principal);
        }

        if (!root.TryGetProperty("roles", out var rolesProp))
            return Task.FromResult(principal);

        var identity = new ClaimsIdentity("Keycloak");
        foreach (var role in rolesProp.EnumerateArray())
        {
            var roleName = role.GetString();
            if (roleName is not null)
                identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
        }

        principal.AddIdentity(identity);
        return Task.FromResult(principal);
    }
}
