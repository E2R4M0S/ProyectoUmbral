using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// JWT Bearer authentication — JWKS cached keyset via ConfigurationManager
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Jwt:Authority"];
        options.Audience = builder.Configuration["Jwt:Audience"];
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
        // ConfigurationManager auto-caches the JWKS with default 5-min refresh
        options.BackchannelHttpHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
    });

// Transform Keycloak realm_access.roles into ClaimTypes.Role claims
builder.Services.AddScoped<IClaimsTransformation, KeycloakRolesTransformer>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("authenticated", policy =>
        policy.RequireAuthenticatedUser());
    options.AddPolicy("admin", policy =>
        policy.RequireRole("admin"));
    options.AddPolicy("operator", policy =>
        policy.RequireRole("operator"));
    options.AddPolicy("participant", policy =>
        policy.RequireRole("participant"));
    options.AddPolicy("operator_or_participant", policy =>
        policy.RequireAssertion(ctx =>
            ctx.User.IsInRole("operator") || ctx.User.IsInRole("participant")));
});

// YARP reverse proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Public health endpoint (no auth)
app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "ApiGateway" }))
   .AllowAnonymous();

// YARP middleware
app.MapReverseProxy();

app.Run();

/// <summary>
/// Maps Keycloak's <c>realm_access.roles</c> claim to <see cref="ClaimTypes.Role"/>
/// so <c>[Authorize(Roles = "...")]</c> and role-based policies work natively.
/// </summary>
file sealed class KeycloakRolesTransformer : IClaimsTransformation
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
