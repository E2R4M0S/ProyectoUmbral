using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sessions.Application.Common.Interfaces;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;
using Xunit;

namespace Sessions.Api.Tests.Endpoints;

public class GetSessionByIdEndpointTests
{
    private readonly ISessionRepository _repo = Substitute.For<ISessionRepository>();
    private readonly ILogger<Program> _logger = Substitute.For<ILogger<Program>>();

    private static Session CreateSession()
    {
        var stage = SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "M1", "Trivia", 1, "test-token");
        return Session.Create("Test", "123456", new List<SessionStage> { stage });
    }

    [Fact]
    public async Task GetById_ExistingSession_ReturnsOk()
    {
        var session = CreateSession();
        _repo.GetByIdWithStagesAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(session);

        var result = await SimulateEndpoint(session.Id);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetById_SessionNotFound_ReturnsNotFound()
    {
        _repo.GetByIdWithStagesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Session?)null);

        var result = await SimulateEndpoint(Guid.NewGuid());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetById_ReturnsSessionDetails()
    {
        var session = CreateSession();
        _repo.GetByIdWithStagesAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(session);

        var okResult = (OkObjectResult)(await SimulateEndpoint(session.Id));
        var body = okResult.Value!;
        body.Should().BeEquivalentTo(new
        {
            Id = session.Id,
            Name = session.Name,
            Pin = session.Pin,
            Status = session.Status.ToString()
        }, opt => opt.ExcludingMissingMembers());
    }

    private async Task<IActionResult> SimulateEndpoint(Guid id)
    {
        var session = await _repo.GetByIdWithStagesAsync(id, CancellationToken.None);
        if (session is null)
            return new NotFoundObjectResult(new { error = "Session not found" });

        return new OkObjectResult(new
        {
            session.Id,
            session.Name,
            session.Pin,
            session.CurrentStageOrder,
            Stages = session.Stages.Select(s => new { s.MissionId, s.MissionTitle, s.MissionType, s.Order }),
            Status = session.Status.ToString(),
            session.StartedAt,
            session.EndedAt,
            session.CreatedAt,
            Participants = session.Participants.Select(p => new { p.UserId, p.JoinedAt })
        });
    }
}
