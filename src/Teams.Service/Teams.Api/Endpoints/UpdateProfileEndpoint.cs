using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Teams.Application.Common.Exceptions;
using Teams.Application.Teams.Profile;

namespace Teams.Api.Endpoints;

public static class UpdateProfileEndpoint
{
    public static void MapUpdateProfileEndpoint(this WebApplication app)
    {
        app.MapPut("/api/teams/profile", async (
            HttpContext context,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            var keycloakUserId = context.User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(keycloakUserId))
            {
                logger.LogWarning("UpdateProfile failed: missing sub claim in JWT");
                return Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Unauthorized",
                    detail: "Missing or invalid JWT token.");
            }

            UpdateProfileCommand? command;
            try
            {
                command = await System.Text.Json.JsonSerializer.DeserializeAsync<UpdateProfileCommand>(
                    context.Request.Body,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                    ct);

                if (command is null)
                {
                    return Results.BadRequest(new
                    {
                        error = "Validation failed",
                        detail = "Invalid request body. Name and Alias are required."
                    });
                }

                command = new UpdateProfileCommand(command.Name, command.Alias, keycloakUserId);
            }
            catch (System.Text.Json.JsonException ex)
            {
                logger.LogWarning(ex, "UpdateProfile failed: invalid request body");
                return Results.BadRequest(new
                {
                    error = "Validation failed",
                    detail = "Invalid request body. Name and Alias are required."
                });
            }

            try
            {
                var response = await mediator.Send(command, ct);
                return Results.Ok(response);
            }
            catch (RegistrationException ex)
            {
                logger.LogWarning(
                    "UpdateProfile conflict: Field={Field}, Message={Message}", ex.Field, ex.Message);
                return Results.Conflict(new
                {
                    error = "Conflict",
                    details = new[]
                    {
                        new { field = ex.Field, message = ex.Message }
                    }
                });
            }
            catch (FluentValidation.ValidationException ex)
            {
                logger.LogWarning(
                    "UpdateProfile validation failed: {Message}", ex.Message);
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
            catch (Exception ex)
            {
                logger.LogError(ex, "UpdateProfile failed for KeycloakUserId={KeycloakUserId}", keycloakUserId);
                return Results.Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Profile update failed",
                    detail: "An unexpected error occurred while updating the profile.");
            }
        })
        .WithName("UpdateProfile")
        .RequireAuthorization("participant");
    }
}
