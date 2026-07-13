using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Sessions.Api.Endpoints;
using Sessions.Application;
using Sessions.Infrastructure;
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
                ValidateIssuer = false,
                ValidateAudience = false,
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
    builder.Services.AddHttpContextAccessor();

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("authenticated", policy =>
            policy.RequireAuthenticatedUser());

        options.AddPolicy("admin", policy =>
            policy.RequireRole("admin"));

        options.AddPolicy("operator_or_admin", policy =>
            policy.RequireAssertion(ctx =>
                ctx.User.IsInRole("admin") || ctx.User.IsInRole("operator")));
    });

    builder.Services.AddHealthChecks();
    builder.Services.AddOpenApi();

    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    var app = builder.Build();

    // Auto-create database on startup for development
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<SessionsDbContext>();
        db.Database.EnsureCreated();
    }

    app.UseSerilogRequestLogging();

    app.UseAuthentication();
    app.UseAuthorization();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "Sessions.Api" }))
       .WithName("Health")
       .AllowAnonymous();

    app.MapCreateSessionEndpoint();
    app.MapGetSessionsEndpoint();
    app.MapGetSessionByIdEndpoint();
    app.MapAdvanceStageEndpoint();
    app.MapTransitionSessionEndpoint();
    app.MapJoinSessionEndpoint();
    app.MapStartSessionEndpoint();
    app.MapFinishSessionEndpoint();
    app.MapGetSessionProgressEndpoint();
    app.MapReleaseClueEndpoint();
    app.MapValidateQrEndpoint();
    app.MapParticipantScoreEndpoints();

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
