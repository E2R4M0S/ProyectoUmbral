using MediatR;
using Microsoft.AspNetCore.Mvc;
using Sessions.Application.Sessions.ValidateQr;

namespace Sessions.Api.Endpoints;

public static class ValidateQrEndpoint
{
    public static void MapValidateQrEndpoint(this WebApplication app)
    {
        app.MapPost("/{sessionId:guid}/validate-qr", async (
            [FromRoute] Guid sessionId,
            [FromBody] ValidateQrRequest request,
            IMediator mediator,
            ILogger<Program> logger,
            HttpContext httpContext) =>
        {
            var userIdClaim = httpContext.User.FindFirst("sub")
                ?? httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
                return Results.Unauthorized();

            var command = new ValidateQrCommand(sessionId, userId, request.StageId, request.Token);
            var result = await mediator.Send(command);

            if (!result.IsValid)
            {
                logger.LogWarning("QR validation rejected: SessionId={SessionId}, Reason={Reason}",
                    sessionId, result.ErrorMessage);
            }

            return Results.Ok(new
            {
                isValid = result.IsValid,
                advanced = result.Advanced,
                currentStageOrder = result.CurrentStageOrder,
                totalStages = result.TotalStages,
                isLastStage = result.IsLastStage,
                isAtGate = result.IsAtGate,
                gateOpened = result.GateOpened,
                gatePosition = result.GatePosition,
                gateThreshold = result.GateThreshold,
                isEliminated = result.IsEliminated,
                errorMessage = result.ErrorMessage,
            });
        })
        .WithName("ValidateQr")
        .RequireAuthorization("authenticated");
    }
}

public record ValidateQrRequest(Guid StageId, string Token);
