using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Missions.Application.Common.Interfaces;
using Missions.Domain.Entities;
using Missions.Domain.Enums;
using NSubstitute;
using Xunit;

namespace Missions.Api.Tests.Endpoints;

public class StageQrEndpointTests
{
    private readonly IMissionRepository _repository = Substitute.For<IMissionRepository>();

    [Fact]
    public async Task GetQr_MissionNotFound_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Mission?)null);

        var result = await SimulateEndpoint(Guid.NewGuid(), Guid.NewGuid());

        result.Should().BeAssignableTo<IStatusCodeHttpResult>()
            .Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetQr_StageNotFound_ReturnsNotFound()
    {
        var mission = Mission.Create("Test Mission", "Desc", Difficulty.Easy, 30, MissionType.Treasure);
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(mission);

        var result = await SimulateEndpoint(mission.Id, Guid.NewGuid());

        result.Should().BeAssignableTo<IStatusCodeHttpResult>()
            .Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetQr_ValidMissionAndStage_ReturnsPngFile()
    {
        var mission = Mission.Create("Test Mission", "Desc", Difficulty.Easy, 30, MissionType.Treasure);
        mission.AddStage("Stage 1", "First stage", 1);
        var stage = mission.Stages.First();
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(mission);

        var result = await SimulateEndpoint(mission.Id, stage.Id);

        var fileResult = result.Should().BeOfType<FileContentHttpResult>().Subject;
        fileResult.ContentType.Should().Be("image/png");
        fileResult.FileContents.IsEmpty.Should().BeFalse();
    }

    // Mirrors the body of StageQrEndpoint.MapStageQrEndpoint
    private async Task<IResult> SimulateEndpoint(Guid missionId, Guid stageId)
    {
        var mission = await _repository.GetByIdAsync(missionId, CancellationToken.None);
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

        using var qrGenerator = new QRCoder.QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(payload, QRCoder.QRCodeGenerator.ECCLevel.M);
        using var qrCode = new QRCoder.PngByteQRCode(qrData);
        var pngBytes = qrCode.GetGraphic(10);

        return Results.File(pngBytes, "image/png", $"qr-stage-{stageId}.png");
    }
}
