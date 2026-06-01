using MediatR;
using Microsoft.Extensions.Logging;
using Trivia.Application.Common.Interfaces;

namespace Trivia.Application.Trivias.Clues;

public class ReleaseClueCommandHandler : IRequestHandler<ReleaseClueCommand>
{
    private readonly IEventPublisher _publisher;
    private readonly ILogger<ReleaseClueCommandHandler> _logger;

    public ReleaseClueCommandHandler(IEventPublisher publisher, ILogger<ReleaseClueCommandHandler> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Handle(ReleaseClueCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Releasing clue for QuizId={QuizId} TeamId={TeamId}", request.QuizId, request.TeamId);
        await _publisher.PublishAsync("ClueReleased", new { QuizId = request.QuizId, TeamId = request.TeamId, ClueData = request.ClueData }, ct);
    }
}
