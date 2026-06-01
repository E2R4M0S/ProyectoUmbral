using MediatR;
using Trivia.Application.Common.Interfaces;

namespace Trivia.Application.Trivias.Leaderboard;

public class UpdateLeaderboardCommandHandler : IRequestHandler<UpdateLeaderboardCommand>
{
    private readonly ILeaderboardRepository _leaderboardRepo;
    private readonly IEventPublisher _publisher;

    public UpdateLeaderboardCommandHandler(ILeaderboardRepository leaderboardRepo, IEventPublisher publisher)
    {
        _leaderboardRepo = leaderboardRepo;
        _publisher = publisher;
    }

    public async Task Handle(UpdateLeaderboardCommand request, CancellationToken ct)
    {
        var existing = await _leaderboardRepo.GetByTeamAsync(request.QuizId, request.TeamId, ct);
        if (existing == null)
        {
            existing = new Trivia.Domain.Entities.LeaderboardEntry { QuizId = request.QuizId, TeamId = request.TeamId, Score = request.Delta };
            await _leaderboardRepo.AddOrUpdateAsync(existing, ct);
        }
        else
        {
            existing.Score += request.Delta;
            await _leaderboardRepo.AddOrUpdateAsync(existing, ct);
        }

        var leaderboard = await _leaderboardRepo.GetByQuizAsync(request.QuizId, ct);
        await _publisher.PublishAsync("LeaderboardUpdated", leaderboard, ct);
    }
}
