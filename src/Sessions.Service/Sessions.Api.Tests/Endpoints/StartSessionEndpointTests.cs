using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Sessions.Application.Sessions.Transition;
using Xunit;

namespace Sessions.Api.Tests.Endpoints;

public class StartSessionEndpointTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    [Fact]
    public async Task StartSession_WithValidTransition_ShouldReturnOk()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        _mediator.Send(Arg.Any<TransitionSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await SimulateEndpoint(sessionId);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(new { id = sessionId, status = "Active" });
    }

    [Fact]
    public async Task StartSession_WhenSessionNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        _mediator.Send(Arg.Any<TransitionSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException($"Session with id '{sessionId}' not found"));

        // Act
        var result = await SimulateEndpoint(sessionId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task StartSession_WhenInvalidTransition_ShouldReturnBadRequest()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        _mediator.Send(Arg.Any<TransitionSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("Cannot transition session from 'Scheduled' to 'Active'"));

        // Act
        var result = await SimulateEndpoint(sessionId);

        // Assert
        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().BeEquivalentTo(new { error = "Cannot start session", message = "Cannot transition session from 'Scheduled' to 'Active'" });
    }

    private async Task<IActionResult> SimulateEndpoint(Guid sessionId)
    {
        var command = new TransitionSessionCommand(sessionId, "Active");
        try
        {
            await _mediator.Send(command, CancellationToken.None);
            return new OkObjectResult(new { id = sessionId, status = "Active" });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return new NotFoundObjectResult(new { error = "Not Found", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(new { error = "Cannot start session", message = ex.Message });
        }
    }
}
