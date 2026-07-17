using FluentAssertions;
using Sessions.Domain.Entities;
using Xunit;

namespace Sessions.Domain.Tests.Entities;

public class SessionParticipantTests
{
    [Fact]
    public void Create_WithValidParams_SetsAllProperties()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var alias = "guerrero42";
        var before = DateTime.UtcNow;

        var participant = SessionParticipant.Create(sessionId, userId, alias);

        participant.Id.Should().NotBe(Guid.Empty);
        participant.SessionId.Should().Be(sessionId);
        participant.UserId.Should().Be(userId);
        participant.UserAlias.Should().Be(alias);
        participant.JoinedAt.Should().BeOnOrAfter(before);
        participant.JoinedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    [Fact]
    public void Create_WithNullAlias_UsesTruncatedUserId()
    {
        var userId = Guid.NewGuid();

        var participant = SessionParticipant.Create(Guid.NewGuid(), userId, null);

        participant.UserAlias.Should().Be(userId.ToString("N")[..8]);
    }

    [Fact]
    public void Create_WithoutAlias_UsesTruncatedUserId()
    {
        var userId = Guid.NewGuid();

        var participant = SessionParticipant.Create(Guid.NewGuid(), userId);

        participant.UserAlias.Should().Be(userId.ToString("N")[..8]);
    }

    [Fact]
    public void Create_GeneratesUniqueIds()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var p1 = SessionParticipant.Create(sessionId, userId);
        var p2 = SessionParticipant.Create(sessionId, userId);

        p1.Id.Should().NotBe(p2.Id);
    }

    [Fact]
    public void Create_SetsCorrectSessionAndUserId()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var participant = SessionParticipant.Create(sessionId, userId, "alias");

        participant.SessionId.Should().Be(sessionId);
        participant.UserId.Should().Be(userId);
    }

    [Fact]
    public void Create_WithEmptyAlias_UsesAliasAsProvided()
    {
        var participant = SessionParticipant.Create(Guid.NewGuid(), Guid.NewGuid(), "");

        participant.UserAlias.Should().Be("");
    }

    [Fact]
    public void Create_MultipleParticipants_AllHaveCorrectSessionId()
    {
        var sessionId = Guid.NewGuid();

        var participants = Enumerable.Range(0, 5)
            .Select(i => SessionParticipant.Create(sessionId, Guid.NewGuid(), $"player{i}"))
            .ToList();

        participants.Should().OnlyContain(p => p.SessionId == sessionId);
        participants.Select(p => p.Id).Distinct().Should().HaveCount(5);
    }

    [Fact]
    public void AddScore_ShouldIncreaseScoreAndSetLastScoreAt()
    {
        var participant = SessionParticipant.Create(Guid.NewGuid(), Guid.NewGuid());
        var before = DateTime.UtcNow;

        participant.AddScore(100);

        participant.Score.Should().Be(100);
        participant.LastScoreAt.Should().NotBeNull();
        participant.LastScoreAt!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void ApplyPenalty_ShouldDecreaseScoreAndSetLastScoreAt()
    {
        var participant = SessionParticipant.Create(Guid.NewGuid(), Guid.NewGuid());
        participant.AddScore(100);

        participant.ApplyPenalty(30);

        participant.Score.Should().Be(70);
        participant.LastScoreAt.Should().NotBeNull();
    }

    [Fact]
    public void ResetScore_ShouldClearScoreAndLastScoreAt()
    {
        var participant = SessionParticipant.Create(Guid.NewGuid(), Guid.NewGuid());
        participant.AddScore(100);

        participant.ResetScore();

        participant.Score.Should().Be(0);
        participant.LastScoreAt.Should().BeNull();
    }
}
