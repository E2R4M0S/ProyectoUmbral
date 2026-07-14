using MediatR;
using Missions.Application.Operators.Disable;

namespace Missions.Api.Endpoints;

public static class DisableOperatorEndpoint
{
    public static void MapDisableOperatorEndpoint(this WebApplication app)
    {
        app.MapPost("/operators/disable", async (
            DisableOperatorCommand command,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            try
            {
                var result = await mediator.Send(command);

                logger.LogInformation(
                    "Operator disabled: Email={Email}, WasAlreadyDisabled={WasAlreadyDisabled}",
                    command.Email, result.WasAlreadyDisabled);

                return Results.Ok(new
                {
                    message = result.Message,
                    wasAlreadyDisabled = result.WasAlreadyDisabled
                });
            }
            catch (FluentValidation.ValidationException ex)
            {
                logger.LogWarning(
                    "Operator disable validation failed: {Message}", ex.Message);

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
            catch (InvalidOperationException ex) when (ex.Message.Contains("No user found"))
            {
                logger.LogWarning(
                    "Operator disable not found: Email={Email}", command.Email);

                return Results.NotFound(new
                {
                    error = "Not Found",
                    details = new[]
                    {
                        new { field = "Email", message = "No se encontró un operador con ese email." }
                    }
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Operator disable failed due to an unexpected error");

                return Results.Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Operator disable failed",
                    detail: "An unexpected error occurred while disabling the operator. Please try again later.");
            }
        })
        .WithName("DisableOperator")
        .RequireAuthorization("admin");
    }
}
