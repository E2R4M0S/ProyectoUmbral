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
            ILogger<Program> logger) =>
        {
            var command = new ValidateQrCommand(sessionId, request.StageId, request.Token);
            var result = await mediator.Send(command);

            if (!result.IsValid)
            {
                logger.LogWarning("QR validation rejected: SessionId={SessionId}, Reason={Reason}",
                    sessionId, result.ErrorMessage);
                return Results.BadRequest(new { error = "QR inválido", message = result.ErrorMessage });
            }

            return Results.Ok(new
            {
                isValid = result.IsValid,
                advanced = result.Advanced,
                currentStageOrder = result.CurrentStageOrder,
                totalStages = result.TotalStages,
                isLastStage = result.IsLastStage
            });
        })
        .WithName("ValidateQr")
        .RequireAuthorization("authenticated");
    }
}

public record ValidateQrRequest(Guid StageId, string Token);
