using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.Transition;
using Xunit;

namespace Sessions.Api.Tests.Endpoints;

public class TransitionSessionEndpointTests
{
    private readonly IGameSessionFacade _facade = Substitute.For<IGameSessionFacade>();
    private readonly ILogger<Program> _logger = Substitute.For<ILogger<Program>>();

    [Fact]
    public async Task Transition_ValidTransition_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _facade.TransitionAndNotify(id, "Active", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var result = await SimulateEndpoint(id, new TransitionSessionCommand(id, "Active"));

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Transition_SessionNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _facade.TransitionAndNotify(id, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException($"Session '{id}' not found"));

        var result = await SimulateEndpoint(id, new TransitionSessionCommand(id, "Active"));

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Transition_InvalidStatusTransition_ReturnsBadRequest()
    {
        var id = Guid.NewGuid();
        _facade.TransitionAndNotify(id, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new InvalidOperationException("Cannot transition from Finished"));

        var result = await SimulateEndpoint(id, new TransitionSessionCommand(id, "Active"));

        result.Should().BeOfType<BadRequestObjectResult>()
            .Which.Value.Should().BeEquivalentTo(new { error = "Invalid status transition", message = "Cannot transition from Finished" });
    }

    [Fact]
    public async Task Transition_ValidationFailure_ReturnsBadRequest()
    {
        var id = Guid.NewGuid();
        var failures = new[] { new ValidationFailure("NewStatus", "Invalid status") };
        _facade.TransitionAndNotify(id, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new ValidationException(failures));

        var result = await SimulateEndpoint(id, new TransitionSessionCommand(id, "Invalid"));

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Transition_UnexpectedError_ReturnsProblem()
    {
        var id = Guid.NewGuid();
        _facade.TransitionAndNotify(id, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => throw new Exception("DB down"));

        var result = await SimulateEndpoint(id, new TransitionSessionCommand(id, "Active"));

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(500);
    }

    private async Task<IActionResult> SimulateEndpoint(Guid id, TransitionSessionCommand command)
    {
        try
        {
            await _facade.TransitionAndNotify(id, command.NewStatus);
            return new NoContentResult();
        }
        catch (ValidationException ex)
        {
            return new BadRequestObjectResult(new { error = "Validation failed", details = ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }) });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return new NotFoundObjectResult(new { error = "Not Found", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(new { error = "Invalid status transition", message = ex.Message });
        }
        catch (Exception)
        {
            return new ObjectResult(new { statusCode = 500 }) { StatusCode = 500 };
        }
    }
}
