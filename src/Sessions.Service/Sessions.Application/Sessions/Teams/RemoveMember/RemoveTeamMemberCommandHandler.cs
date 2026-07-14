using MediatR;
using Microsoft.Extensions.Logging;
using Sessions.Application.Common.Interfaces;

namespace Sessions.Application.Sessions.Teams.RemoveMember;

public class RemoveTeamMemberCommandHandler : IRequestHandler<RemoveTeamMemberCommand>
{
    private readonly ISessionRepository _repository;
    private readonly ILogger<RemoveTeamMemberCommandHandler> _logger;

    public RemoveTeamMemberCommandHandler(
        ISessionRepository repository,
        ILogger<RemoveTeamMemberCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task Handle(RemoveTeamMemberCommand command, CancellationToken ct)
    {
        var team = await _repository.GetTeamByIdAsync(command.TeamId, ct)
            ?? throw new InvalidOperationException($"Equipo '{command.TeamId}' no encontrado.");

        if (team.SessionId != command.SessionId)
            throw new InvalidOperationException("El equipo no pertenece a esta sesión.");

        team.RemoveMember(command.UserId);
        await _repository.UpdateTeamAsync(team, ct);

        _logger.LogInformation(
            "User {UserId} removed from team {TeamId} in session {SessionId}",
            command.UserId, command.TeamId, command.SessionId);
    }
}
