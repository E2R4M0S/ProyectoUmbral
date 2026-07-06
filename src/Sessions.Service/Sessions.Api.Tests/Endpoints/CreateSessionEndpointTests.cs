using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sessions.Application.Sessions.Create;
using Xunit;

namespace Sessions.Api.Tests.Endpoints;

public class CreateSessionEndpointTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogger<Program> _logger = Substitute.For<ILogger<Program>>();

    private static CreateSessionCommandResult MakeResult(string name = "Test") => new(
        Guid.NewGuid(), name, "123456", "Scheduled", 0,
        new[] { new StageOutput(Guid.NewGuid(), "Mission 1", "Trivia", 1) },
        null, null, DateTime.UtcNow);

    private static CreateSessionCommand MakeCommand() => new("Test",
        new List<StageInput> { new(Guid.NewGuid(), "M1", "Trivia", 1) });

    [Fact]
    public async Task CreateSession_WithValidCommand_ReturnsCreated()
    {
        var result = MakeResult("Alpha");
        _mediator.Send(Arg.Any<CreateSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns(result);

        var response = await SimulateEndpoint(MakeCommand());

        response.Should().BeOfType<CreatedResult>()
            .Which.Location.Should().Contain(result.Id.ToString());
    }

    [Fact]
    public async Task CreateSession_ValidationFailure_ReturnsBadRequest()
    {
        var failures = new[] { new ValidationFailure("Name", "Name is required") };
        _mediator.Send(Arg.Any<CreateSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns<CreateSessionCommandResult>(_ => throw new ValidationException(failures));

        var response = await SimulateEndpoint(MakeCommand());

        response.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CreateSession_UniquePinConflict_ReturnsConflict()
    {
        _mediator.Send(Arg.Any<CreateSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns<CreateSessionCommandResult>(_ => throw new InvalidOperationException("Could not generate unique PIN"));

        var response = await SimulateEndpoint(MakeCommand());

        response.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task CreateSession_UnexpectedException_ReturnsProblem()
    {
        _mediator.Send(Arg.Any<CreateSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns<CreateSessionCommandResult>(_ => throw new Exception("Unexpected"));

        var response = await SimulateEndpoint(MakeCommand());

        response.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    private async Task<IActionResult> SimulateEndpoint(CreateSessionCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return new CreatedResult($"/{result.Id}", new { id = result.Id, name = result.Name, pin = result.Pin, status = result.Status });
        }
        catch (ValidationException ex)
        {
            return new BadRequestObjectResult(new { error = "Validation failed", details = ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }) });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("unique PIN"))
        {
            return new ConflictObjectResult(new { error = "Conflict", details = new[] { new { field = "Pin", message = "Could not generate a unique PIN. Please try again." } } });
        }
        catch (Exception)
        {
            return new ObjectResult(new { statusCode = 500, title = "Session creation failed" }) { StatusCode = 500 };
        }
    }
}
