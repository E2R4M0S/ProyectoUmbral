using MediatR;
using Microsoft.Extensions.Logging;
using Trivia.Application.Common.Interfaces;
using Trivia.Application.Trivias.Questions;

namespace Trivia.Application.Trivias.Leaderboard;

public class CloseQuestionCommandHandler : IRequestHandler<CloseQuestionCommand>
{
    private readonly IParticipantAnswerRepository _participantRepo;
    private readonly ILeaderboardRepository _leaderboardRepo;
    private readonly IMediator _mediator;
    private readonly IEventPublisher _publisher;
    private readonly ILogger<CloseQuestionCommandHandler> _logger;

    public CloseQuestionCommandHandler(IParticipantAnswerRepository participantRepo, ILeaderboardRepository leaderboardRepo, IMediator mediator, IEventPublisher publisher, ILogger<CloseQuestionCommandHandler> logger)
    {
        _participantRepo = participantRepo;
        _leaderboardRepo = leaderboardRepo;
        _mediator = mediator;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Handle(CloseQuestionCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Closing question {QuestionId} for quiz {QuizId}", request.QuestionId, request.QuizId);

        // Reuse existing leaderboard consolidation logic if applicable. For brevity we'll compute per-team deltas as in previous handlers.
        var allAnswers = await _participantRepo.GetByQuizAsync(request.QuizId, ct);
        var filtered = allAnswers.Where(a => a.QuestionId == request.QuestionId).ToList();

        var grouped = filtered.GroupBy(a => a.TeamId);
        foreach (var g in grouped)
        {
            var correctCount = g.Count(a => a.IsCorrect);
            var delta = correctCount * 10;
            if (delta == 0) continue;

            var existing = await _leaderboardRepo.GetByTeamAsync(request.QuizId, g.Key, ct);
            if (existing == null)
            {
                existing = new Trivia.Domain.Entities.LeaderboardEntry { QuizId = request.QuizId, TeamId = g.Key, Score = delta };
                await _leaderboardRepo.AddOrUpdateAsync(existing, ct);
            }
            else
            {
                existing.Score += delta;
                await _leaderboardRepo.AddOrUpdateAsync(existing, ct);
            }
        }

        // publish leaderboard snapshot
        var leaderboard = await _leaderboardRepo.GetByQuizAsync(request.QuizId, ct);
        await _publisher.PublishAsync("LeaderboardUpdated", leaderboard, ct);

        // After we close the question, compute and publish QuestionResults via mediator so logic is centralized
        try
        {
            await _mediator.Send(new QuestionResultsCommand(request.QuizId, request.QuestionId), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to trigger QuestionResultsCommand after closing question");
        }
    }
}
