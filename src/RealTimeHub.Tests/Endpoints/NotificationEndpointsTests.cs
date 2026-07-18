using FluentAssertions;
using RealTimeHub.Endpoints;
using Xunit;

namespace RealTimeHub.Tests.Endpoints;

public class NotificationEndpointsTests
{
    [Fact]
    public void SessionStatusNotification_RecordsCanBeCreated()
    {
        var sessionId = Guid.NewGuid();
        var notification = new SessionStatusNotification(sessionId, "Active");

        notification.SessionId.Should().Be(sessionId);
        notification.Status.Should().Be("Active");
    }

    [Fact]
    public void ProgressNotification_RecordsCanBeCreated()
    {
        var sessionId = Guid.NewGuid();
        var notification = new ProgressNotification(sessionId, new { Percent = 75 });

        notification.SessionId.Should().Be(sessionId);
        notification.ProgressData.Should().NotBeNull();
    }

    [Fact]
    public void ClueReleasedNotification_WithTeamId_RecordsCanBeCreated()
    {
        var sessionId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var notification = new ClueReleasedNotification(sessionId, teamId, null, new { ClueId = Guid.NewGuid() });

        notification.SessionId.Should().Be(sessionId);
        notification.TeamId.Should().Be(teamId);
    }

    [Fact]
    public void ClueReleasedNotification_WithoutTeamId_RecordsCanBeCreated()
    {
        var sessionId = Guid.NewGuid();
        var notification = new ClueReleasedNotification(sessionId, null, null, new { ClueId = Guid.NewGuid() });

        notification.SessionId.Should().Be(sessionId);
        notification.TeamId.Should().BeNull();
    }

    [Fact]
    public void ClueReleasedNotification_WithUserId_RecordsCanBeCreated()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var notification = new ClueReleasedNotification(sessionId, null, userId, new { ClueId = Guid.NewGuid() });

        notification.SessionId.Should().Be(sessionId);
        notification.TeamId.Should().BeNull();
        notification.UserId.Should().Be(userId);
    }

    [Fact]
    public void QuestionAskedNotification_RecordsCanBeCreated()
    {
        var sessionId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var notification = new QuestionAskedNotification(
            sessionId,
            questionId,
            "What is 2+2?",
            new[] { "3", "4", "5", "6" },
            30,
            DateTime.UtcNow);

        notification.SessionId.Should().Be(sessionId);
        notification.QuestionId.Should().Be(questionId);
        notification.QuestionText.Should().Be("What is 2+2?");
        notification.Options.Should().HaveCount(4);
        notification.TimeLimitSeconds.Should().Be(30);
    }

    [Fact]
    public void LeaderboardEntryDto_RecordsCanBeCreated()
    {
        var dto = new LeaderboardEntryDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Team Alpha",
            1500,
            DateTime.UtcNow);

        dto.TeamName.Should().Be("Team Alpha");
        dto.Score.Should().Be(1500);
    }

    [Fact]
    public void QuestionClosedNotification_RecordsCanBeCreated()
    {
        var sessionId = Guid.NewGuid();
        var questionId = Guid.NewGuid();
        var correctAnswerId = Guid.NewGuid();

        var notification = new QuestionClosedNotification(sessionId, questionId, correctAnswerId, "Four");

        notification.SessionId.Should().Be(sessionId);
        notification.QuestionId.Should().Be(questionId);
        notification.CorrectAnswerId.Should().Be(correctAnswerId);
        notification.CorrectAnswerText.Should().Be("Four");
    }

    [Fact]
    public void RankingUpdatedNotification_RecordsCanBeCreated()
    {
        var sessionId = Guid.NewGuid();
        var ranking = new List<RankingEntryDto>
        {
            new(1, "Team A", 100),
            new(2, "Team B", 80)
        };

        var notification = new RankingUpdatedNotification(sessionId, ranking);

        notification.SessionId.Should().Be(sessionId);
        notification.Ranking.Should().HaveCount(2);
        notification.Ranking[0].Position.Should().Be(1);
        notification.Ranking[0].TeamName.Should().Be("Team A");
        notification.Ranking[0].Score.Should().Be(100);
    }
}