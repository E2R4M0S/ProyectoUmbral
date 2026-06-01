namespace RealTimeHub.Endpoints;

public record LeaderboardEntryDto(Guid QuizId, Guid TeamId, int Score, DateTime UpdatedAt);
