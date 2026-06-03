using Trivia.Api.Endpoints;
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

    builder.Services.AddAuthentication().AddJwtBearer();
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("operator_or_admin", policy =>
            policy.RequireAssertion(ctx =>
                ctx.User.IsInRole("admin") || ctx.User.IsInRole("operator")));
    });

    builder.Services.AddHealthChecks();
    builder.Services.AddOpenApi();

    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "Trivia.Api" }))
       .WithName("Health")
       .AllowAnonymous();

    app.MapRankingEndpoints();

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
