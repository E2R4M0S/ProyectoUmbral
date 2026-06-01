using MediatR;
using Microsoft.Extensions.Logging;
using Trivia.Application.Common.Interfaces;

namespace Trivia.Application.Trivias.Progress;

public class UpdateProgressCommandHandler : IRequestHandler<UpdateProgressCommand>
{
    private readonly IEventPublisher _publisher;
    private readonly ILogger<UpdateProgressCommandHandler> _logger;

    public UpdateProgressCommandHandler(IEventPublisher publisher, ILogger<UpdateProgressCommandHandler> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Handle(UpdateProgressCommand request, CancellationToken ct)
    {
        _logger.LogInformation("Updating progress for QuizId={QuizId}", request.QuizId);
        await _publisher.PublishAsync("ProgressUpdated", new { QuizId = request.QuizId, ProgressData = request.ProgressData }, ct);
    }
}
