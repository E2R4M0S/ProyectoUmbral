using MediatR;
using Missions.Application.Common.Exceptions;
using Missions.Application.Participants.Register;

namespace Missions.Api.Endpoints;

public static class RegisterParticipantEndpoint
{
    public static void MapRegisterParticipantEndpoint(this WebApplication app)
    {
        app.MapPost("/api/register", async (
            RegisterParticipantCommand command,
            IMediator mediator,
            ILogger<Program> logger) =>
        {
            try
            {
                var participantId = await mediator.Send(command);

                logger.LogInformation(
                    "Participant registered successfully: {ParticipantId}", participantId);

                return Results.Created(
                    $"/api/participants/{participantId}",
                    new { id = participantId, message = "Registration successful" });
            }
            catch (RegistrationException ex)
            {
                logger.LogWarning(
                    "Registration conflict: {Field} — {Message}", ex.Field, ex.Message);

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
                    "Registration validation failed: {Message}", ex.Message);

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
                logger.LogError(ex, "Registration failed due to an unexpected error");

                return Results.Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Registration failed",
                    detail: "An unexpected error occurred during registration. Please try again later.");
            }
        })
        .WithName("RegisterParticipant")
        .AllowAnonymous();
    }
}
