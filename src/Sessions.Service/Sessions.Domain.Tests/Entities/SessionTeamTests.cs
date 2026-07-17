using FluentAssertions;
using Sessions.Domain.Entities;
using Xunit;

namespace Sessions.Domain.Tests.Entities;

public class SessionTeamTests
{
    [Fact]
    public void Create_WithValidName_SetsAllProperties()
    {
        var sessionId = Guid.NewGuid();

        var team = SessionTeam.Create(sessionId, "Los Piratas");

        team.Id.Should().NotBe(Guid.Empty);
        team.SessionId.Should().Be(sessionId);
        team.Name.Should().Be("Los Piratas");
        team.MaxMembers.Should().Be(5);
        team.Score.Should().Be(0);
        team.LastScoreAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrow()
    {
        Action act = () => SessionTeam.Create(Guid.NewGuid(), "");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddScore_ShouldIncreaseScoreAndSetLastScoreAt()
    {
        var team = SessionTeam.Create(Guid.NewGuid(), "Team A");
        var before = DateTime.UtcNow;

        team.AddScore(50);

        team.Score.Should().Be(50);
        team.LastScoreAt.Should().NotBeNull();
        team.LastScoreAt!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void ApplyPenalty_ShouldDecreaseScoreAndSetLastScoreAt()
    {
        var team = SessionTeam.Create(Guid.NewGuid(), "Team A");
        team.AddScore(50);

        team.ApplyPenalty(20);

        team.Score.Should().Be(30);
        team.LastScoreAt.Should().NotBeNull();
    }

    [Fact]
    public void ApplyPenalty_ShouldNotDropScoreBelowZero()
    {
        var team = SessionTeam.Create(Guid.NewGuid(), "Team A");
        team.AddScore(10);

        team.ApplyPenalty(50);

        team.Score.Should().Be(0);
    }

    [Fact]
    public void ResetScore_ShouldClearScoreAndLastScoreAt()
    {
        var team = SessionTeam.Create(Guid.NewGuid(), "Team A");
        team.AddScore(50);

        team.ResetScore();

        team.Score.Should().Be(0);
        team.LastScoreAt.Should().BeNull();
    }

    [Fact]
    public void AddMember_ShouldAddMemberToTeam()
    {
        var team = SessionTeam.Create(Guid.NewGuid(), "Team A");
        var userId = Guid.NewGuid();

        team.AddMember(userId, "alias");

        team.Members.Should().ContainSingle(m => m.UserId == userId);
    }

    [Fact]
    public void AddMember_WhenAlreadyMember_ShouldThrow()
    {
        var team = SessionTeam.Create(Guid.NewGuid(), "Team A");
        var userId = Guid.NewGuid();
        team.AddMember(userId, "alias");

        Action act = () => team.AddMember(userId, "alias");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddMember_WhenTeamFull_ShouldThrow()
    {
        var team = SessionTeam.Create(Guid.NewGuid(), "Team A");
        for (int i = 0; i < 5; i++)
            team.AddMember(Guid.NewGuid(), $"member{i}");

        Action act = () => team.AddMember(Guid.NewGuid(), "extra");

        act.Should().Throw<InvalidOperationException>();
    }
}
