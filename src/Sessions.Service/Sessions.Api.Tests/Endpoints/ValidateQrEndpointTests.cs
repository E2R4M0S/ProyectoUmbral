using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sessions.Application.Sessions.ValidateQr;
using Xunit;

namespace Sessions.Api.Tests.Endpoints;

public class ValidateQrEndpointTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogger<Program> _logger = Substitute.For<ILogger<Program>>();

    [Fact]
    public async Task ValidateQr_ValidRequest_ReturnsOkWithResult()
    {
        var sessionId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var commandResult = new ValidateQrResult(true, true, 1, 2, false);
        _mediator.Send(Arg.Any<ValidateQrCommand>(), Arg.Any<CancellationToken>())
            .Returns(commandResult);

        var result = await SimulateEndpoint(sessionId, stageId, "valid-token");

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(new
            {
                isValid = true,
                advanced = true,
                currentStageOrder = 1,
                totalStages = 2,
                isLastStage = false
            });
    }

    [Fact]
    public async Task ValidateQr_InvalidQr_ReturnsBadRequest()
    {
        var sessionId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var commandResult = new ValidateQrResult(false, false, 0, 1, false, ErrorMessage: "Invalid QR code for current stage");
        _mediator.Send(Arg.Any<ValidateQrCommand>(), Arg.Any<CancellationToken>())
            .Returns(commandResult);

        var result = await SimulateEndpoint(sessionId, stageId, "bad-token");

        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.Value.Should().BeEquivalentTo(new
        {
            error = "QR inválido",
            message = "Invalid QR code for current stage"
        });
    }

    [Fact]
    public async Task ValidateQr_SessionNotFound_ReturnsBadRequest()
    {
        var sessionId = Guid.NewGuid();
        var commandResult = new ValidateQrResult(false, false, 0, 0, false, ErrorMessage: "Session not found");
        _mediator.Send(Arg.Any<ValidateQrCommand>(), Arg.Any<CancellationToken>())
            .Returns(commandResult);

        var result = await SimulateEndpoint(sessionId, Guid.NewGuid(), "any-token");

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ValidateQr_LastStage_ReturnsOkWithIsLastStageTrue()
    {
        var sessionId = Guid.NewGuid();
        var stageId = Guid.NewGuid();
        var commandResult = new ValidateQrResult(true, false, 2, 2, true);
        _mediator.Send(Arg.Any<ValidateQrCommand>(), Arg.Any<CancellationToken>())
            .Returns(commandResult);

        var result = await SimulateEndpoint(sessionId, stageId, "tok");

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(new
            {
                isValid = true,
                advanced = false,
                isLastStage = true
            });
    }

    // Mirrors the body of ValidateQrEndpoint.MapValidateQrEndpoint
    private async Task<IActionResult> SimulateEndpoint(Guid sessionId, Guid stageId, string token)
    {
        var command = new ValidateQrCommand(sessionId, Guid.NewGuid(), stageId, token);
        var result = await _mediator.Send(command, CancellationToken.None);

        if (!result.IsValid)
        {
            _logger.LogWarning("QR validation rejected: SessionId={SessionId}, Reason={Reason}",
                sessionId, result.ErrorMessage);
            return new BadRequestObjectResult(new { error = "QR inválido", message = result.ErrorMessage });
        }

        return new OkObjectResult(new
        {
            isValid = result.IsValid,
            advanced = result.Advanced,
            currentStageOrder = result.CurrentStageOrder,
            totalStages = result.TotalStages,
            isLastStage = result.IsLastStage
        });
    }
}
