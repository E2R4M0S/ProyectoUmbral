using Trivia.Application;
using Trivia.Infrastructure;
using Serilog;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

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

    // Authentication (JWT) - basic configuration for Keycloak-compatible tokens
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            // Note: these values should be set by environment or configuration in real deployments
            options.Authority = builder.Configuration["Keycloak:Authority"] ?? "http://keycloak:8080/auth/realms/umbral";
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false
            };
        });

    builder.Services.AddAuthorization();

    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    // Ensure DB exists (development convenience)
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<Trivia.Infrastructure.TriviaDbContext>();
        db.EnsureDatabaseCreated();
    }

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Service = "Trivia.Api" }))
       .WithName("Health")
       .AllowAnonymous();

    // Trivia endpoints
    var mediator = app.Services.GetRequiredService<MediatR.IMediator>();
    var adminRole = Environment.GetEnvironmentVariable("TRIVIA_ADMIN_ROLE") ?? "admin";

    app.MapPost("/api/trivia/quiz", async (Trivia.Application.Quizzes.Commands.CreateQuizCommand cmd) =>
    {
        try
        {
            var id = await mediator.Send(cmd);
            return Results.Created($"/api/trivia/quiz/{id}", new { id });
        }
        catch (InvalidOperationException e) { return Results.Conflict(e.Message); }
    }).RequireAuthorization();

    app.MapDelete("/api/trivia/quiz/{id:guid}", async (Guid id) =>
    {
        try
        {
            await mediator.Send(new Trivia.Application.Quizzes.Commands.DeleteQuizCommand(id));
            return Results.NoContent();
        }
        catch (KeyNotFoundException) { return Results.NotFound(); }
    }).RequireAuthorization();

    app.MapGet("/api/trivia/quiz", async (string? q, int page, int size) =>
    {
        var res = await mediator.Send(new Trivia.Application.Quizzes.Queries.GetQuizzesQuery(q, page == 0 ? 1 : page, size == 0 ? 20 : size));
        return Results.Ok(res);
    });

    app.MapGet("/api/trivia/quiz/{id:guid}", async (Guid id) =>
    {
        var res = await mediator.Send(new Trivia.Application.Quizzes.Queries.GetQuizByIdQuery(id));
        return res is null ? Results.NotFound() : Results.Ok(res);
    });

    app.MapPost("/api/trivia/quiz/{quizId:guid}/questions", async (Guid quizId, Trivia.Application.Questions.Commands.AddQuestionCommand cmd) =>
    {
        try
        {
            var id = await mediator.Send(cmd with { QuizId = quizId });
            return Results.Created($"/api/trivia/quiz/{quizId}/questions/{id}", new { id });
        }
        catch (KeyNotFoundException) { return Results.NotFound(); }
        catch (InvalidOperationException e) { return Results.Conflict(e.Message); }
    }).RequireAuthorization();

    app.MapPut("/api/trivia/quiz/{quizId:guid}/questions/{questionId:guid}", async (Guid quizId, Guid questionId, Trivia.Application.Questions.Commands.EditQuestionCommand cmd) =>
    {
        try
        {
            await mediator.Send(cmd with { QuizId = quizId, QuestionId = questionId });
            return Results.NoContent();
        }
        catch (KeyNotFoundException) { return Results.NotFound(); }
        catch (InvalidOperationException e) { return Results.Conflict(e.Message); }
    }).RequireAuthorization();

    app.MapDelete("/api/trivia/quiz/{quizId:guid}/questions/{questionId:guid}", async (Guid quizId, Guid questionId) =>
    {
        try
        {
            await mediator.Send(new Trivia.Application.Questions.Commands.DeleteQuestionCommand(quizId, questionId));
            return Results.NoContent();
        }
        catch (KeyNotFoundException) { return Results.NotFound(); }
    }).RequireAuthorization();

    app.MapPost("/api/trivia/quiz/{quizId:guid}/questions/{questionId:guid}/answers", async (Guid quizId, Guid questionId, Trivia.Application.Answers.Commands.AddAnswerCommand cmd) =>
    {
        try
        {
            var id = await mediator.Send(cmd with { QuizId = quizId, QuestionId = questionId });
            return Results.Created($"/api/trivia/quiz/{quizId}/questions/{questionId}/answers/{id}", new { id });
        }
        catch (KeyNotFoundException) { return Results.NotFound(); }
        catch (InvalidOperationException e) { return Results.Conflict(e.Message); }
        catch (ArgumentException) { return Results.BadRequest(); }
    }).RequireAuthorization();

    app.MapPatch("/api/trivia/quiz/{quizId:guid}/questions/{questionId:guid}/correct", async (Guid quizId, Guid questionId, Trivia.Application.Answers.Commands.MarkCorrectAnswerCommand cmd) =>
    {
        try
        {
            await mediator.Send(cmd with { QuizId = quizId, QuestionId = questionId });
            return Results.NoContent();
        }
        catch (KeyNotFoundException) { return Results.NotFound(); }
    }).RequireAuthorization();

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

