using MediatR;

namespace Trivia.Application.Trivias.Leaderboard;

public record UpdateLeaderboardCommand(Guid QuizId, Guid TeamId, int Delta) : IRequest;
