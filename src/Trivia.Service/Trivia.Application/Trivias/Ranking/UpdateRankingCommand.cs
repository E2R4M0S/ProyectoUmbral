using MediatR;

namespace Trivia.Application.Trivias.Ranking;

public record UpdateRankingCommand(Guid SessionId) : IRequest<List<RankingEntryDto>>;

public record RankingEntryDto(int Position, string TeamName, int Score);
