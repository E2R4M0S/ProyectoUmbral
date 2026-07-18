using MediatR;
using Sessions.Application.Common.Interfaces;

namespace Sessions.Application.Sessions.Clues;

public class ReleaseClueCommandHandler : IRequestHandler<ReleaseClueCommand>
{
    private readonly IGameNotifier _notifier;

    public ReleaseClueCommandHandler(IGameNotifier notifier)
    {
        _notifier = notifier;
    }

    public async Task Handle(ReleaseClueCommand command, CancellationToken ct)
    {
        // Clue release notification
        // The actual clue data would come from Missions.Service in a full integration
        var clueData = new
        {
            ClueId = command.ClueId,
            ReleasedAt = DateTime.UtcNow
        };

        await _notifier.NotifyClueReleased(command.SessionId, command.TeamId, null, clueData, ct);
    }
}
