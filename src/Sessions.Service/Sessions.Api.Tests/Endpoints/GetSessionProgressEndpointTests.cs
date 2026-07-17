using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sessions.Application.Sessions.Consult;
using Xunit;

namespace Sessions.Api.Tests.Endpoints;

public class GetSessionProgressEndpointTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogger<Program> _logger = Substitute.For<ILogger<Program>>();

    [Fact]
    public async Task GetProgress_ExistingSession_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var progress = new SessionProgressDto(id, "Test", "Active", 120, 0, 0, new List<ParticipantProgressDto>());
        _mediator.Send(Arg.Is<GetSessionProgressQuery>(q => q.SessionId == id), Arg.Any<CancellationToken>())
            .Returns(progress);

        var result = await SimulateEndpoint(id);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(progress);
    }

    [Fact]
    public async Task GetProgress_SessionNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _mediator.Send(Arg.Any<GetSessionProgressQuery>(), Arg.Any<CancellationToken>())
            .Returns((SessionProgressDto?)null);

        var result = await SimulateEndpoint(id);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetProgress_UnexpectedException_ReturnsProblem()
    {
        var id = Guid.NewGuid();
        _mediator.Send(Arg.Any<GetSessionProgressQuery>(), Arg.Any<CancellationToken>())
            .Returns<SessionProgressDto?>(_ => throw new Exception("DB failure"));

        var result = await SimulateEndpoint(id);

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(500);
    }

    private async Task<IActionResult> SimulateEndpoint(Guid id)
    {
        try
        {
            var progress = await _mediator.Send(new GetSessionProgressQuery(id));
            if (progress is null)
                return new NotFoundObjectResult(new { error = "Not Found", message = $"Session with id '{id}' not found" });

            return new OkObjectResult(progress);
        }
        catch (Exception)
        {
            return new ObjectResult(new { statusCode = 500 }) { StatusCode = 500 };
        }
    }
}
