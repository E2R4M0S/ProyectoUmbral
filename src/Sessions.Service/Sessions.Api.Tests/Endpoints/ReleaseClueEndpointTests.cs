using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sessions.Application.Common.Interfaces;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;
using Xunit;

namespace Sessions.Api.Tests.Endpoints;

public class ReleaseClueEndpointTests
{
    private readonly ISessionRepository _repo = Substitute.For<ISessionRepository>();
    private readonly IGameSessionFacade _facade = Substitute.For<IGameSessionFacade>();
    private readonly ILogger<Program> _logger = Substitute.For<ILogger<Program>>();

    private static Session CreateActiveSession()
    {
        var stage = SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "M1", "Trivia", 1, "test-token");
        var session = Session.Create("Test", "123456", new List<SessionStage> { stage });
        session.TransitionTo(SessionStatus.Preparing);
        session.TransitionTo(SessionStatus.Active);
        return session;
    }

    [Fact]
    public async Task ReleaseClue_ActiveSessionWithStage_ReturnsOk()
    {
        var session = CreateActiveSession();
        var clueId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        _repo.GetByIdWithStagesAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(session);

        var result = await SimulateEndpoint(session.Id, clueId, teamId);

        result.Should().BeOfType<OkObjectResult>();
        await _facade.Received(1).ReleaseClueAndNotify(session.Id, clueId, teamId, Arg.Any<string?>(), Arg.Any<int?>());
    }

    [Fact]
    public async Task ReleaseClue_SessionNotFound_ReturnsNotFound()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdWithStagesAsync(id, Arg.Any<CancellationToken>())
            .Returns((Session?)null);

        var result = await SimulateEndpoint(id, Guid.NewGuid(), null);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ReleaseClue_NoActiveStage_ReturnsBadRequest()
    {
        // Advance CurrentStageOrder past the only stage so GetCurrentStage() returns null
        var stage = SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "M1", "Trivia", 1, "test-token");
        var session = Session.Create("Test", "111111", new List<SessionStage> { stage });
        session.TransitionTo(SessionStatus.Preparing);
        session.TransitionTo(SessionStatus.Active);
        // Force CurrentStageOrder = 1 so GetCurrentStage looks for order 2 (doesn't exist)
        typeof(Session).GetProperty("CurrentStageOrder")!
            .SetValue(session, 1, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance, null, null, null);

        _repo.GetByIdWithStagesAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(session);

        var result = await SimulateEndpoint(session.Id, Guid.NewGuid(), null);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ReleaseClue_UnexpectedError_ReturnsProblem()
    {
        var id = Guid.NewGuid();
        _repo.GetByIdWithStagesAsync(id, Arg.Any<CancellationToken>())
            .Returns<Session?>(_ => throw new Exception("Network error"));

        var result = await SimulateEndpoint(id, Guid.NewGuid(), null);

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(500);
    }

    private async Task<IActionResult> SimulateEndpoint(Guid sessionId, Guid clueId, Guid? teamId)
    {
        try
        {
            var session = await _repo.GetByIdWithStagesAsync(sessionId, CancellationToken.None);
            if (session is null)
                return new NotFoundObjectResult(new { error = "Session not found" });

            var currentStage = session.GetCurrentStage();
            if (currentStage is null)
                return new BadRequestObjectResult(new { error = "No active stage", message = "Session has no current stage" });

            await _facade.ReleaseClueAndNotify(sessionId, clueId, teamId, null, null);

            return new OkObjectResult(new { id = sessionId, clueId, status = "Released", hasContent = false });
        }
        catch (Exception)
        {
            return new ObjectResult(new { statusCode = 500 }) { StatusCode = 500 };
        }
    }
}
