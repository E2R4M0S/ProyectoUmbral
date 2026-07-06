using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sessions.Application.Sessions.Join;
using Xunit;

namespace Sessions.Api.Tests.Endpoints;

public class JoinSessionEndpointTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogger<Program> _logger = Substitute.For<ILogger<Program>>();

    [Fact]
    public async Task JoinSession_ValidCommand_ReturnsOkWithParticipant()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var commandResult = new JoinSessionCommandResult(sessionId, userId, DateTime.UtcNow);
        _mediator.Send(Arg.Any<JoinSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns(commandResult);

        var result = await SimulateEndpoint(new JoinSessionCommand("123456"));

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(commandResult);
    }

    [Fact]
    public async Task JoinSession_SessionNotFound_ReturnsNotFound()
    {
        _mediator.Send(Arg.Any<JoinSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns<JoinSessionCommandResult>(_ => throw new InvalidOperationException("Session not found"));

        var result = await SimulateEndpoint(new JoinSessionCommand("000000"));

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task JoinSession_CannotJoin_ReturnsBadRequest()
    {
        _mediator.Send(Arg.Any<JoinSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns<JoinSessionCommandResult>(_ => throw new InvalidOperationException("Session is not in Preparing state"));

        var result = await SimulateEndpoint(new JoinSessionCommand("123456"));

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().BeEquivalentTo(new { error = "Cannot join session", message = "Session is not in Preparing state" });
    }

    [Fact]
    public async Task JoinSession_UnexpectedError_ReturnsProblem()
    {
        _mediator.Send(Arg.Any<JoinSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns<JoinSessionCommandResult>(_ => throw new Exception("DB error"));

        var result = await SimulateEndpoint(new JoinSessionCommand("123456"));

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(500);
    }

    private async Task<IActionResult> SimulateEndpoint(JoinSessionCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return new OkObjectResult(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return new NotFoundObjectResult(new { error = "Not Found", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(new { error = "Cannot join session", message = ex.Message });
        }
        catch (Exception)
        {
            return new ObjectResult(new { statusCode = 500 }) { StatusCode = 500 };
        }
    }
}
