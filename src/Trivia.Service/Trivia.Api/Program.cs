using Trivia.Api.Endpoints;
using Trivia.Application;
using Trivia.Infrastructure;
using Serilog;

try
{
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .CreateBootstrapLogger();
}
catch { }

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration));

    builder.Services.AddHealthChecks();
    builder.Services.AddOpenApi();

    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = builder.Configuration["Jwt:Authority"];
            options.Audience = builder.Configuration["Jwt:Audience"];
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
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

    builder.Services.AddScoped<Microsoft.AspNetCore.Authentication.IClaimsTransformation, KeycloakRolesTransformer>();
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("admin", policy => policy.RequireRole("admin"));
        options.AddPolicy("operator_or_admin", policy =>
            policy.RequireAssertion(ctx => ctx.User.IsInRole("admin") || ctx.User.IsInRole("operator")));
    });

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    if (!app.Environment.IsEnvironment("Test"))
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Trivia.Infrastructure.TriviaDbContext>();
            db.Database.EnsureCreated();
        }
    }

    app.UseAuthentication();
    app.UseAuthorization();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "Trivia.Api" }))
       .WithName("Health")
       .AllowAnonymous();

    app.MapGameEndpoints();
    app.MapStartTrivia();
    app.MapProgressEndpoints();
    app.MapAnsweringEndpoints();
    app.MapRankingEndpoints();
    app.MapQuestionEndpoints();
    app.MapQuizBankEndpoints();
    app.MapInternalCloseQuestion();
    app.MapInternalQuestionResults();

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
/// Maps Keycloak's realm_access.roles claim into ClaimTypes.Role so role-based policies work.
/// </summary>
file sealed class KeycloakRolesTransformer : Microsoft.AspNetCore.Authentication.IClaimsTransformation
{
    public Task<System.Security.Claims.ClaimsPrincipal> TransformAsync(System.Security.Claims.ClaimsPrincipal principal)
    {
        var realmAccess = principal.FindFirst("realm_access")?.Value;
        if (realmAccess is null)
            return Task.FromResult(principal);

        System.Text.Json.JsonElement root;
        try
        {
            root = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(realmAccess);
        }
        catch (System.Text.Json.JsonException)
        {
            return Task.FromResult(principal);
        }

        if (!root.TryGetProperty("roles", out var rolesProp))
            return Task.FromResult(principal);

        var identity = new System.Security.Claims.ClaimsIdentity("Keycloak");
        foreach (var role in rolesProp.EnumerateArray())
        {
            var roleName = role.GetString();
            if (roleName is not null)
                identity.AddClaim(new System.Security.Claims.Claim(
                    System.Security.Claims.ClaimTypes.Role, roleName));
        }

        principal.AddIdentity(identity);
        return Task.FromResult(principal);
    }
}

public partial class Program { }
