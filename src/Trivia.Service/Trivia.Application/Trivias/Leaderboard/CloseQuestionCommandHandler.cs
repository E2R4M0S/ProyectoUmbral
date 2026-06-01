using MediatR;
using Microsoft.Extensions.Logging;
using Trivia.Application.Common.Interfaces;

namespace Trivia.Application.Trivias.Leaderboard;

public class CloseQuestionCommandHandler : IRequestHandler<CloseQuestionCommand>
{
    private readonly IParticipantAnswerRepository _participantRepo;
    private readonly ILeaderboardRepository _leaderboardRepo;
    private readonly IEventPublisher _publisher;
    private readonly ILogger<CloseQuestionCommandHandler> _logger;

    public CloseQuestionCommandHandler(IParticipantAnswerRepository participantRepo, ILeaderboardRepository leaderboardRepo, IEventPublisher publisher, ILogger<CloseQuestionCommandHandler> logger)
    {
        _participantRepo = participantRepo;
        _leaderboardRepo = leaderboardRepo;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Handle(CloseQuestionCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Closing question for QuizId={QuizId}", request.QuizId);

        // Load all participant answers for the quiz
        // The ParticipantAnswerRepository currently only exposes AddAsync; use DbContext via leaderboardRepo
        // But to respect interfaces, we'll attempt to cast leaderboard repo to access DbContext indirectly via implementation.

        // As a pragmatic approach: query leaderboard deltas by reading ParticipantAnswers from the TriviaDbContext
        // Implementation detail: leaderboardRepo is used to persist deltas, participant answers are read via casting to concrete implementation

        try
        {
            // Read persisted participant answers for this quiz
            var allAnswers = await _participantRepo.GetByQuizAsync(request.QuizId, ct);
            if (allAnswers == null)
            {
                _logger.LogWarning("ParticipantAnswer repository returned null list for quiz {QuizId}", request.QuizId);
                allAnswers = new List<Domain.Entities.ParticipantAnswer>();
            }

            // Filter answers for this quiz and compute per-team deltas
            var grouped = allAnswers.Where(a => a.QuizId == request.QuizId).GroupBy(a => a.TeamId);
            foreach (var g in grouped)
            {
                var correctCount = g.Count(a => a.IsCorrect);
                var delta = correctCount * 10; // 10 points per correct answer
                if (delta == 0) continue;

                // Use UpdateLeaderboardCommand to apply the delta
                var cmd = new UpdateLeaderboardCommand(request.QuizId, g.Key, delta);
                // Directly call handler via repository for simplicity
                var existing = await _leaderboardRepo.GetByTeamAsync(cmd.QuizId, cmd.TeamId, ct);
                if (existing == null)
                {
                    existing = new Domain.Entities.LeaderboardEntry { QuizId = cmd.QuizId, TeamId = cmd.TeamId, Score = cmd.Delta };
                    await _leaderboardRepo.AddOrUpdateAsync(existing, ct);
                }
                else
                {
                    existing.Score += cmd.Delta;
                    await _leaderboardRepo.AddOrUpdateAsync(existing, ct);
                }
            }

            // Publish final snapshot
            var leaderboard = await _leaderboardRepo.GetByQuizAsync(request.QuizId, ct);
            await _publisher.PublishAsync("LeaderboardUpdated", leaderboard, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to close question for QuizId={QuizId}", request.QuizId);
        }
    }
}
