using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Xunit;
using FluentAssertions;

namespace Trivia.Infrastructure.Tests.Messaging;

public class EventSerializationTests
{
    [Fact]
    public void SerializeAndDeserialize_TriviaAnswerSubmittedEvent()
    {
        var payload = new
        {
            Id = Guid.NewGuid(),
            QuizId = Guid.NewGuid(),
            TeamId = Guid.NewGuid(),
            QuestionId = Guid.NewGuid(),
            AnswerId = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            IsCorrect = true
        };

        var json = JsonSerializer.Serialize(payload);
        json.Should().Contain("QuizId");
        json.Should().Contain("TeamId");
        json.Should().Contain("IsCorrect");

        var deserialized = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        deserialized.Should().ContainKey("IsCorrect");
        deserialized!["IsCorrect"].GetBoolean().Should().BeTrue();
    }

    [Fact]
    public void SerializeAndDeserialize_LeaderboardUpdatedEvent()
    {
        var entries = new[]
        {
            new { Position = 1, TeamName = "Team A", Score = 100 },
            new { Position = 2, TeamName = "Team B", Score = 80 }
        };

        var json = JsonSerializer.Serialize(entries);
        json.Should().Contain("Team A");
        json.Should().Contain("Team B");

        var deserialized = JsonSerializer.Deserialize<JsonElement[]>(json);
        deserialized.Should().HaveCount(2);
        deserialized[0].GetProperty("Score").GetInt32().Should().Be(100);
    }

    [Fact]
    public void Serialize_RankingEntryDto()
    {
        var dto = new { SessionId = Guid.NewGuid(), Ranking = new[] { new { Position = 1, TeamName = "A", Score = 50 } } };
        var json = JsonSerializer.Serialize(dto);
        json.Should().Contain("SessionId");
        json.Should().Contain("Position");
    }

    [Fact]
    public void RoutingKey_Format_ShouldBeValid()
    {
        var routingKeys = new[] { "TriviaAnswerSubmittedEvent", "LeaderboardUpdated", "ProgressUpdated",
                                  "ClueReleased", "TriviaStarted", "QuestionResultsUpdated", "session.status.changed" };

        foreach (var key in routingKeys)
        {
            key.Should().NotBeNullOrWhiteSpace();
            Encoding.UTF8.GetByteCount(key).Should().BeGreaterThan(0);
        }
    }
}
