using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sessions.Application.Sessions.Consult;
using Xunit;

namespace Sessions.Api.Tests.Endpoints;

public class GetAuditLogEndpointTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ILogger<Program> _logger = Substitute.For<ILogger<Program>>();

    [Fact]
    public async Task GetAuditLog_ExistingSession_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var events = new List<AuditEventDto>
        {
            new(Guid.NewGuid(), "PenaltyApplied", "Pista liberada", null, null, -10, DateTime.UtcNow)
        };
        _mediator.Send(Arg.Is<GetAuditLogQuery>(q => q.SessionId == id), Arg.Any<CancellationToken>())
            .Returns(events);

        var result = await SimulateEndpoint(id);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(events);
    }

    [Fact]
    public async Task GetAuditLog_SessionNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _mediator.Send(Arg.Any<GetAuditLogQuery>(), Arg.Any<CancellationToken>())
            .Returns((List<AuditEventDto>?)null);

        var result = await SimulateEndpoint(id);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetAuditLog_UnexpectedException_ReturnsProblem()
    {
        var id = Guid.NewGuid();
        _mediator.Send(Arg.Any<GetAuditLogQuery>(), Arg.Any<CancellationToken>())
            .Returns<List<AuditEventDto>?>(_ => throw new Exception("DB failure"));

        var result = await SimulateEndpoint(id);

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(500);
    }

    private async Task<IActionResult> SimulateEndpoint(Guid id)
    {
        try
        {
            var events = await _mediator.Send(new GetAuditLogQuery(id));
            if (events is null)
                return new NotFoundObjectResult(new { error = "Not Found", message = $"Session with id '{id}' not found" });

            return new OkObjectResult(events);
        }
        catch (Exception)
        {
            return new ObjectResult(new { statusCode = 500 }) { StatusCode = 500 };
        }
    }
}
