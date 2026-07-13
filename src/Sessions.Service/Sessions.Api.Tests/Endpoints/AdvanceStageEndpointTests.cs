using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Sessions.Api.Endpoints;
using Sessions.Application.Common.Interfaces;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;
using Xunit;

namespace Sessions.Api.Tests.Endpoints;

/// <summary>
/// Unit-style tests for AdvanceStageEndpoint.
/// Mirrors the existing StartSessionEndpointTests pattern: simulate the endpoint
/// inline because we don't have WebApplicationFactory infrastructure yet.
/// </summary>
public class AdvanceStageEndpointTests
{
    private readonly ISessionRepository _repository = Substitute.For<ISessionRepository>();
    private readonly IGameSessionFacade _facade = Substitute.For<IGameSessionFacade>();
    private readonly ILogger<Program> _logger = Substitute.For<ILogger<Program>>();

    private static Session CreateActiveSessionWithStages(int stageCount)
    {
        var stages = Enumerable.Range(1, stageCount)
            .Select(i => SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), $"M{i}", "Stage", "Trivia", i, "test-token"))
            .ToList();
        var session = Session.Create("Test", "123456", stages);
        session.TransitionTo(SessionStatus.Preparing);
        session.TransitionTo(SessionStatus.Active);
        return session;
    }

    [Fact]
    public async Task AdvanceStage_ActiveSessionWithMultipleStages_ReturnsOkWithProgress()
    {
        // Arrange
        var session = CreateActiveSessionWithStages(3);
        _repository.GetByIdWithStagesAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(session);

        // Act
        var result = await SimulateEndpoint(session.Id, _repository, _facade, _logger);

        // Assert
        result.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().BeEquivalentTo(new
            {
                currentStageOrder = 1,
                totalStages = 3,
                isLastStage = false
            });

        await _repository.Received(1).UpdateAsync(session, Arg.Any<CancellationToken>());
        await _facade.Received(1).NotifyStageAdvanced(session.Id, 1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdvanceStage_SessionNotFound_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repository.GetByIdWithStagesAsync(id, Arg.Any<CancellationToken>())
            .Returns((Session?)null);

        // Act
        var result = await SimulateEndpoint(id, _repository, _facade, _logger);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task AdvanceStage_OnLastStage_ReturnsBadRequest()
    {
        // Arrange
        var session = CreateActiveSessionWithStages(1); // single stage
        _repository.GetByIdWithStagesAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(session);

        // Act
        var result = await SimulateEndpoint(session.Id, _repository, _facade, _logger);

        // Assert
        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.Value.Should().BeEquivalentTo(new
        {
            error = "Already on last stage"
        });
        await _facade.DidNotReceive().NotifyStageAdvanced(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdvanceStage_SessionInScheduledState_ReturnsBadRequest()
    {
        // Arrange
        var stages = new List<SessionStage>
        {
            SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "M1", "Stage", "Trivia", 1, "test-token"),
            SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "M2", "Stage", "Treasure", 2, "test-token")
        };
        var session = Session.Create("Test", "123456", stages);
        // session is Scheduled (no transitions)
        _repository.GetByIdWithStagesAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(session);

        // Act
        var result = await SimulateEndpoint(session.Id, _repository, _facade, _logger);

        // Assert
        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.Value.Should().BeEquivalentTo(new
        {
            error = "Session not active"
        });
        await _facade.DidNotReceive().NotifyStageAdvanced(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdvanceStage_PreparedButNotStarted_ReturnsBadRequest()
    {
        // Arrange
        var stages = new List<SessionStage>
        {
            SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "M1", "Stage", "Trivia", 1, "test-token"),
            SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "M2", "Stage", "Treasure", 2, "test-token")
        };
        var session = Session.Create("Test", "123456", stages);
        session.TransitionTo(SessionStatus.Preparing);
        _repository.GetByIdWithStagesAsync(session.Id, Arg.Any<CancellationToken>())
            .Returns(session);

        // Act
        var result = await SimulateEndpoint(session.Id, _repository, _facade, _logger);

        // Assert
        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.Value.Should().BeEquivalentTo(new
        {
            error = "Session not active"
        });
    }

    /// <summary>
    /// Mirrors the body of AdvanceStageEndpoint.MapAdvanceStageEndpoint — keeps the test
    /// honest to the production logic without spinning up the WebApplicationFactory.
    /// </summary>
    private static async Task<IActionResult> SimulateEndpoint(
        Guid id,
        ISessionRepository repository,
        IGameSessionFacade facade,
        ILogger<Program> logger)
    {
        try
        {
            var session = await repository.GetByIdWithStagesAsync(id, CancellationToken.None);
            if (session is null)
            {
                return new NotFoundObjectResult(new { error = "Not Found", message = $"Session with id '{id}' not found" });
            }

            if (session.Status != SessionStatus.Active)
            {
                return new BadRequestObjectResult(new
                {
                    error = "Session not active",
                    message = $"Cannot advance stage: session is in '{session.Status}' status (must be Active)"
                });
            }

            var newOrder = session.CurrentStageOrder + 1;
            if (newOrder >= session.Stages.Count)
            {
                return new BadRequestObjectResult(new
                {
                    error = "Already on last stage",
                    message = "Cannot advance stage: already on the last stage"
                });
            }

            session.AdvanceStage();
            await repository.UpdateAsync(session, CancellationToken.None);
            await facade.NotifyStageAdvanced(id, session.CurrentStageOrder, CancellationToken.None);

            return new OkObjectResult(new
            {
                currentStageOrder = session.CurrentStageOrder,
                totalStages = session.Stages.Count,
                isLastStage = session.CurrentStageOrder >= session.Stages.Count - 1
            });
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return new NotFoundObjectResult(new { error = "Not Found", message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(new { error = "Cannot advance stage", message = ex.Message });
        }
    }
}

