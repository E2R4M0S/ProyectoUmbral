using MediatR;
using Missions.Application.Operators.Enable;

namespace Missions.Api.Endpoints;

public static class EnableOperatorEndpoint
{
    public static void MapEnableOperatorEndpoint(this WebApplication app)
    {
        app.MapPost("/operators/enable", async (
            EnableOperatorCommand command,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            try
            {
                var result = await mediator.Send(command);

                logger.LogInformation(
                    "Operator enabled: Email={Email}, WasAlreadyEnabled={WasAlreadyEnabled}",
                    command.Email, result.WasAlreadyEnabled);

                return Results.Ok(new
                {
                    message = result.Message,
                    wasAlreadyEnabled = result.WasAlreadyEnabled
                });
            }
            catch (FluentValidation.ValidationException ex)
            {
                logger.LogWarning(
                    "Operator enable validation failed: {Message}", ex.Message);

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
                    "Operator enable not found: Email={Email}", command.Email);

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
                logger.LogError(ex, "Operator enable failed due to an unexpected error");

                return Results.Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Operator enable failed",
                    detail: "An unexpected error occurred while enabling the operator. Please try again later.");
            }
        })
        .WithName("EnableOperator")
        .RequireAuthorization("admin");
    }
}
