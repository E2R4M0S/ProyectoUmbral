using MediatR;
using Teams.Application.Teams.Operators.Create;

namespace Teams.Api.Endpoints;

public static class CreateOperatorEndpoint
{
    public static void MapCreateOperatorEndpoint(this WebApplication app)
    {
        app.MapPost("/operators", async (
            CreateOperatorCommand command,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            try
            {
                var result = await mediator.Send(command);

                logger.LogInformation(
                    "Operator created successfully: KeycloakUserId={KeycloakUserId}, Email={Email}",
                    result.KeycloakUserId, result.Email);

                return Results.Created(
                    $"/api/admin/operators/{result.KeycloakUserId}",
                    new
                    {
                        name = result.Name,
                        email = result.Email,
                        keycloakUserId = result.KeycloakUserId,
                        message = $"Operator created. An email has been sent to {result.Email} to set their password."
                    });
            }
            catch (FluentValidation.ValidationException ex)
            {
                logger.LogWarning(
                    "Operator creation validation failed: {Message}", ex.Message);

                return Results.BadRequest(new
                {
                    error = "Validation failed",
                    details = ex.Errors.Select(e => new
                    {
                        field = e.PropertyName,
                        message = e.ErrorMessage
                    })
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already registered"))
            {
                logger.LogWarning(
                    "Operator creation conflict: Email already exists — {Message}", ex.Message);

                return Results.Conflict(new
                {
                    error = "Conflict",
                    details = new[]
                    {
                        new { field = "Email", message = "Email is already registered." }
                    }
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operator creation failed due to an unexpected error");

                var problem = new
                {
                    statusCode = StatusCodes.Status500InternalServerError,
                    title = "Operator creation failed",
                    detail = "An unexpected error occurred while creating the operator. Please try again later."
                };

                return Results.Problem(
                    problem.title,
                    null,
                    problem.statusCode,
                    problem.title,
                    problem.detail);
            }
        })
        .WithName("CreateOperator")
        .RequireAuthorization("admin");
    }
}
