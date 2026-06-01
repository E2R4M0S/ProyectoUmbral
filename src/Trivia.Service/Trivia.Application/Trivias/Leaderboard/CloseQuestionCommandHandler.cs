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
            // Try to obtain the EF Core context from the leaderboard repo implementation
            var ctxField = _leaderboardRepo.GetType().GetProperty("Db", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            List<Domain.Entities.ParticipantAnswer>? allAnswers = null;

            // Fallback: try to get method GetByQuizAsync on participantRepo if it's implemented
            var participantRepoType = _participantRepo.GetType();
            var getAllMethod = participantRepoType.GetMethod("GetByQuizAsync");
            if (getAllMethod != null)
            {
                var task = (Task)getAllMethod.Invoke(_participantRepo, new object[] { request.QuizId, ct })!;
                await task;
                var resultProp = task.GetType().GetProperty("Result");
                allAnswers = resultProp?.GetValue(task) as List<Domain.Entities.ParticipantAnswer>;
            }

            // If the participantRepo didn't provide a reader, try reading via leaderboard repo backing context
            if (allAnswers == null)
            {
                // Try to find a property that holds the TriviaDbContext
                var dbProp = _leaderboardRepo.GetType().GetProperty("_db", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (dbProp != null)
                {
                    var db = dbProp.GetValue(_leaderboardRepo);
                    var setMethod = db?.GetType().GetMethod("Set")?.MakeGenericMethod(typeof(Domain.Entities.ParticipantAnswer));
                    if (setMethod != null)
                    {
                        var dbset = setMethod.Invoke(db, null);
                        var toListAsync = typeof(Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions).GetMethod("ToListAsync", new[] { typeof(System.Linq.IQueryable<>).MakeGenericType(typeof(Domain.Entities.ParticipantAnswer)), typeof(CancellationToken) });
                        if (toListAsync != null)
                        {
                            var queryable = dbset as System.Linq.IQueryable<Domain.Entities.ParticipantAnswer>;
                            if (queryable != null)
                            {
                                var task = (Task)toListAsync.MakeGenericMethod(typeof(Domain.Entities.ParticipantAnswer)).Invoke(null, new object[] { queryable, ct })!;
                                await task;
                                var resultProp = task.GetType().GetProperty("Result");
                                allAnswers = resultProp?.GetValue(task) as List<Domain.Entities.ParticipantAnswer>;
                            }
                        }
                    }
                }
            }

            // As a last resort, try reading ParticipantAnswers via reflection on ParticipantAnswerRepository backing field
            if (allAnswers == null)
            {
                var repoDbProp = _participantRepo.GetType().GetField("_db", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (repoDbProp != null)
                {
                    var db = repoDbProp.GetValue(_participantRepo);
                    var setMethod = db?.GetType().GetMethod("Set")?.MakeGenericMethod(typeof(Domain.Entities.ParticipantAnswer));
                    if (setMethod != null)
                    {
                        var dbset = setMethod.Invoke(db, null);
                        var toListAsync = typeof(Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions).GetMethod("ToListAsync", new[] { typeof(System.Linq.IQueryable<>).MakeGenericType(typeof(Domain.Entities.ParticipantAnswer)), typeof(CancellationToken) });
                        if (toListAsync != null)
                        {
                            var queryable = dbset as System.Linq.IQueryable<Domain.Entities.ParticipantAnswer>;
                            if (queryable != null)
                            {
                                var task = (Task)toListAsync.MakeGenericMethod(typeof(Domain.Entities.ParticipantAnswer)).Invoke(null, new object[] { queryable.Where(a => a.QuizId == request.QuizId), ct })!;
                                await task;
                                var resultProp = task.GetType().GetProperty("Result");
                                allAnswers = resultProp?.GetValue(task) as List<Domain.Entities.ParticipantAnswer>;
                            }
                        }
                    }
                }
            }

            if (allAnswers == null)
            {
                _logger.LogWarning("Could not read participant answers when closing question for quiz {QuizId}", request.QuizId);
                return;
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
