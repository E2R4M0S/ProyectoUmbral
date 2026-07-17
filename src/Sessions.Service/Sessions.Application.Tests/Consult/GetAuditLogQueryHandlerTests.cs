using FluentAssertions;
using NSubstitute;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.Consult;
using Sessions.Domain.Entities;
using Xunit;

namespace Sessions.Application.Tests.Consult;

public class GetAuditLogQueryHandlerTests
{
    private readonly ISessionRepository _repository = Substitute.For<ISessionRepository>();
    private readonly GetAuditLogQueryHandler _sut;

    public GetAuditLogQueryHandlerTests()
    {
        _sut = new GetAuditLogQueryHandler(_repository);
    }

    [Fact]
    public async Task Handle_SessionNotFound_ShouldReturnNull()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Session?)null);

        var result = await _sut.Handle(new GetAuditLogQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_SessionExists_ShouldReturnMappedAuditEvents()
    {
        var sessionId = Guid.NewGuid();
        var session = Session.Create("Test", "123456", new List<SessionStage> { SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Mission", "Stage", "Trivia", 1, "test-token") });
        _repository.GetByIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns(session);

        var teamId = Guid.NewGuid();
        var evt = SessionAuditEvent.Create(sessionId, SessionAuditEventTypes.PenaltyApplied, "Pista liberada", teamId, null, -10);
        _repository.GetAuditTrailAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(new List<SessionAuditEvent> { evt });

        var result = await _sut.Handle(new GetAuditLogQuery(sessionId), CancellationToken.None);

        result.Should().NotBeNull();
        result.Should().ContainSingle();
        result![0].Id.Should().Be(evt.Id);
        result[0].EventType.Should().Be(SessionAuditEventTypes.PenaltyApplied);
        result[0].Description.Should().Be("Pista liberada");
        result[0].TeamId.Should().Be(teamId);
        result[0].ScoreDelta.Should().Be(-10);
    }
}
