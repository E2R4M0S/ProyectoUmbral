using Missions.Application.Common.Interfaces;
using QRCoder;

namespace Missions.Api.Endpoints;

public static class StageQrEndpoint
{
    public static void MapStageQrEndpoint(this WebApplication app)
    {
        app.MapGet("/{missionId:guid}/stages/{stageId:guid}/qr", async (
            Guid missionId,
            Guid stageId,
            IMissionRepository repository,
            CancellationToken ct) =>
        {
            var mission = await repository.GetByIdAsync(missionId, ct);
            if (mission is null)
                return Results.NotFound(new { error = "Mission not found" });

            var stage = mission.Stages.FirstOrDefault(s => s.Id == stageId);
            if (stage is null)
                return Results.NotFound(new { error = "Stage not found" });

            var payload = System.Text.Json.JsonSerializer.Serialize(new
            {
                stageId = stage.Id,
                token = stage.QrToken
            });

            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
            using var qrCode = new PngByteQRCode(qrData);
            var pngBytes = qrCode.GetGraphic(10);

            return Results.File(pngBytes, "image/png", $"qr-stage-{stageId}.png");
        })
        .WithName("GetStageQr")
        .RequireAuthorization("operator_or_admin");
    }
}
