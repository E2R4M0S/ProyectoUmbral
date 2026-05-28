using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Teams.Api.Endpoints;
using Teams.Application;
using Teams.Infrastructure;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration));

    // JWT Bearer authentication — validates tokens from Keycloak
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
            options.BackchannelHttpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };
        });

    // Transform Keycloak realm_access.roles into ClaimTypes.Role claims
    builder.Services.AddScoped<IClaimsTransformation, KeycloakRolesTransformer>();

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("participant", policy =>
            policy.RequireRole("participant"));

        options.AddPolicy("admin", policy =>
            policy.RequireRole("admin"));
    });

    builder.Services.AddHealthChecks();
    builder.Services.AddOpenApi();

    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    var app = builder.Build();

    // Auto-create database on startup for development
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<TeamsDbContext>();
        db.Database.EnsureCreated();
    }

    app.UseSerilogRequestLogging();

    app.UseAuthentication();
    app.UseAuthorization();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "Teams.Api" }))
       .WithName("Health")
       .AllowAnonymous();

    app.MapRegisterParticipantEndpoint();

    app.MapGetProfileEndpoint();
    app.MapUpdateProfileEndpoint();

    app.MapCreateOperatorEndpoint();

    app.MapDisableOperatorEndpoint();

    app.MapGetUsersEndpoint();

    app.MapGetUserByIdEndpoint();

    app.MapCreateTeamEndpoint();

    app.MapGetTeamsEndpoint();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

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
