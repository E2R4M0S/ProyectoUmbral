using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Sessions.Application.Sessions.Consult;
using Xunit;

namespace Sessions.Api.Tests.Endpoints;

public class GetSessionsEndpointTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();

    private static GetSessionsResult MakeResult(int count = 2)
    {
        var sessions = Enumerable.Range(1, count)
            .Select(i => new SessionListItemDto(Guid.NewGuid(), $"Session {i}", "M1", "Trivia", 0, 1, $"{i:D6}", 0, "Scheduled", null, null, DateTime.UtcNow))
            .ToList();
        return new GetSessionsResult(sessions, count, 1, 10);
    }

    [Fact]
    public async Task GetSessions_ReturnsOkWithResult()
    {
        var expected = MakeResult(3);
        _mediator.Send(Arg.Any<GetSessionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await SimulateEndpoint(null, null, null, 1, 10);

        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be(expected);
    }

    [Fact]
    public async Task GetSessions_WithStatusFilter_PassesFilterToQuery()
    {
        var expected = MakeResult(1);
        _mediator.Send(Arg.Is<GetSessionsQuery>(q => q.Status == "Active"), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await SimulateEndpoint(null, "Active", null, 1, 10);

        result.Should().BeOfType<OkObjectResult>();
        await _mediator.Received(1).Send(
            Arg.Is<GetSessionsQuery>(q => q.Status == "Active"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSessions_EmptyResult_ReturnsOkWithEmptyList()
    {
        var empty = new GetSessionsResult(new List<SessionListItemDto>(), 0, 1, 10);
        _mediator.Send(Arg.Any<GetSessionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(empty);

        var result = await SimulateEndpoint(null, null, null, 1, 10);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var value = (GetSessionsResult)ok.Value!;
        value.Items.Should().BeEmpty();
    }

    private async Task<IActionResult> SimulateEndpoint(string? search, string? status, Guid? missionId, int page, int pageSize)
    {
        var query = new GetSessionsQuery(search, status, missionId, page, pageSize);
        var result = await _mediator.Send(query, CancellationToken.None);
        return new OkObjectResult(result);
    }
}
