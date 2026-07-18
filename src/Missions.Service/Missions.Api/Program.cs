using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Missions.Api.Endpoints;
using Missions.Application;
using Missions.Infrastructure;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration));

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

    builder.Services.AddScoped<IClaimsTransformation, KeycloakRolesTransformer>();

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("admin", policy =>
            policy.RequireRole("admin"));

        options.AddPolicy("operator_or_admin", policy =>
            policy.RequireAssertion(ctx =>
                ctx.User.IsInRole("admin") || ctx.User.IsInRole("operator")));

        options.AddPolicy("participant", policy =>
            policy.RequireRole("participant"));
    });

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddHealthChecks();
    builder.Services.AddOpenApi();

    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<MissionsDbContext>();
        db.Database.EnsureCreated();
    }

    app.UseSerilogRequestLogging();

    app.UseAuthentication();
    app.UseAuthorization();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "Missions.Api" }))
       .WithName("Health")
       .AllowAnonymous();

    // Mission CRUD
    app.MapCreateMissionEndpoint();
    app.MapUpdateMissionEndpoint();
    app.MapChangeMissionStatusEndpoint();
    app.MapMissionCatalogEndpoints();
    app.MapCreateStageEndpoint();
    app.MapDeleteStageEndpoint();
    app.MapUpdateStageEndpoint();
    app.MapCreateClueEndpoint();
    app.MapDeleteClueEndpoint();
    app.MapStageQrEndpoint();

    // Participant registration and profile
    app.MapRegisterParticipantEndpoint();
    app.MapGetProfileEndpoint();
    app.MapUpdateProfileEndpoint();

    // Operator management
    app.MapCreateOperatorEndpoint();
    app.MapDisableOperatorEndpoint();
    app.MapEnableOperatorEndpoint();

    // User listing
    app.MapGetUsersEndpoint();
    app.MapGetUserByIdEndpoint();

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
/// so role-based policies work natively.
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
