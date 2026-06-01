using Trivia.Application;
using Trivia.Infrastructure;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration));

    builder.Services.AddHealthChecks();
    builder.Services.AddOpenApi();

    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);
    
    // JWT Bearer authentication — validates tokens from Keycloak (aligned with other services)
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

    // Map Keycloak realm_access.roles into ClaimTypes.Role claims like other services
    builder.Services.AddScoped<Microsoft.AspNetCore.Authentication.IClaimsTransformation, Trivia.Api.KeycloakRolesTransformer>();
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("admin", policy => policy.RequireRole("admin"));
        options.AddPolicy("operator_or_admin", policy =>
            policy.RequireAssertion(ctx => ctx.User.IsInRole("admin") || ctx.User.IsInRole("operator")));
    });

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    // Auto-create database on startup for development (keeps behavior aligned with other services)
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<Trivia.Infrastructure.TriviaDbContext>();
        db.Database.EnsureCreated();
    }

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "Trivia.Api" }))
       .WithName("Health")
       .AllowAnonymous();

    // Trivia endpoints
    app.MapStartTrivia();
    app.MapProgressEndpoints();
    app.MapAnsweringEndpoints();

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

