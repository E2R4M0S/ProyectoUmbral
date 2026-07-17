using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.Consult;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;
using Xunit;

namespace Sessions.Application.Tests.Consult;

public class GetMySessionsQueryHandlerTests
{
    private readonly ISessionRepository _repository = Substitute.For<ISessionRepository>();
    private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
    private readonly GetMySessionsQueryHandler _sut;

    public GetMySessionsQueryHandlerTests()
    {
        _sut = new GetMySessionsQueryHandler(_repository, _httpContextAccessor);
    }

    private void SetAuthenticatedUser(Guid userId)
    {
        var identity = new ClaimsIdentity(new[] { new Claim("sub", userId.ToString()) }, "Test");
        _httpContextAccessor.HttpContext.Returns(new DefaultHttpContext { User = new ClaimsPrincipal(identity) });
    }

    [Fact]
    public async Task Handle_WhenNoAuthenticatedUser_ShouldReturnEmptyList()
    {
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        var result = await _sut.Handle(new GetMySessionsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldMapEachSessionWithTheCallersOwnScoreAndJoinedAt()
    {
        var userId = Guid.NewGuid();
        SetAuthenticatedUser(userId);

        var session = Session.Create("My Session", "111222", new List<SessionStage>
        {
            SessionStage.Create(Guid.NewGuid(), Guid.NewGuid(), "Mission A", "Stage", "Trivia", 1, "tok")
        });
        session.TransitionTo(SessionStatus.Preparing);
        session.AddParticipant(userId);
        session.AddParticipant(Guid.NewGuid()); // another participant, must not leak their score

        var me = session.Participants.Single(p => p.UserId == userId);
        me.AddScore(150);

        _repository.GetSessionsForParticipantAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new List<Session> { session });

        var result = await _sut.Handle(new GetMySessionsQuery(), CancellationToken.None);

        result.Should().ContainSingle();
        var dto = result[0];
        dto.Id.Should().Be(session.Id);
        dto.Name.Should().Be("My Session");
        dto.Status.Should().Be("Preparing");
        dto.MissionTitles.Should().Contain("Mission A");
        dto.MyScore.Should().Be(150);
        dto.JoinedAt.Should().Be(me.JoinedAt);
    }
}
