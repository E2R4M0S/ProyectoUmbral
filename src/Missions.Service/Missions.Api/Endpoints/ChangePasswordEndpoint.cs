using FluentValidation;
using MediatR;
using Missions.Application.Participants.Password;

namespace Missions.Api.Endpoints;

public static class ChangePasswordEndpoint
{
    public static void MapChangePasswordEndpoint(this WebApplication app)
    {
        app.MapPut("/api/profile/password", async (
            HttpContext context,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            var keycloakUserId = context.User.FindFirst("sub")?.Value
                ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(keycloakUserId))
            {
                logger.LogWarning("ChangePassword failed: missing sub claim in JWT");
                return Results.Problem(
                    statusCode: StatusCodes.Status401Unauthorized,
                    title: "Unauthorized",
                    detail: "Missing or invalid JWT token.");
            }

            var email = context.User.FindFirst("email")?.Value
                ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                logger.LogWarning(
                    "ChangePassword failed: missing email claim for KeycloakUserId={KeycloakUserId}",
                    keycloakUserId);
                return Results.Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Bad Request",
                    detail: "Email claim not found in JWT token.");
            }

            ChangePasswordRequest? request;
            try
            {
                request = await System.Text.Json.JsonSerializer.DeserializeAsync<ChangePasswordRequest>(
                    context.Request.Body,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                    ct);

                if (request is null || string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
                {
                    return Results.BadRequest(new
                    {
                        error = "Validation failed",
                        detail = "Current password and new password are required."
                    });
                }
            }
            catch (System.Text.Json.JsonException ex)
            {
                logger.LogWarning(ex, "ChangePassword failed: invalid request body");
                return Results.BadRequest(new
                {
                    error = "Validation failed",
                    detail = "Invalid request body. Current password and new password are required."
                });
            }

            var command = new ChangePasswordCommand(
                request.CurrentPassword,
                request.NewPassword,
                keycloakUserId,
                email);

            try
            {
                await mediator.Send(command, ct);
                return Results.NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                logger.LogWarning(
                    "ChangePassword unauthorized: KeycloakUserId={KeycloakUserId}, Message={Message}",
                    keycloakUserId, ex.Message);
                return Results.Json(
                    new { error = "Forbidden", detail = ex.Message },
                    statusCode: StatusCodes.Status403Forbidden);
            }
            catch (ValidationException ex)
            {
                logger.LogWarning(
                    "ChangePassword validation failed: KeycloakUserId={KeycloakUserId}, Message={Message}",
                    keycloakUserId, ex.Message);
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
                logger.LogError(ex, "ChangePassword failed for KeycloakUserId={KeycloakUserId}", keycloakUserId);
                return Results.Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Password change failed",
                    detail: "An unexpected error occurred while changing the password.");
            }
        })
        .WithName("ChangePassword")
        .RequireAuthorization("participant");
    }

    private record ChangePasswordRequest(string CurrentPassword, string NewPassword);
}
