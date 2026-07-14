using MediatR;
using Microsoft.Extensions.Logging;
using Sessions.Application.Common.Interfaces;
using Sessions.Domain.Entities;
using Sessions.Domain.Enums;

namespace Sessions.Application.Sessions.Teams.CreateTeam;

public class CreateSessionTeamCommandHandler
    : IRequestHandler<CreateSessionTeamCommand, CreateSessionTeamResult>
{
    private readonly ISessionRepository _repository;
    private readonly ILogger<CreateSessionTeamCommandHandler> _logger;

    public CreateSessionTeamCommandHandler(
        ISessionRepository repository,
        ILogger<CreateSessionTeamCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<CreateSessionTeamResult> Handle(
        CreateSessionTeamCommand command,
        CancellationToken ct)
    {
        var session = await _repository.GetByIdAsync(command.SessionId, ct)
            ?? throw new InvalidOperationException($"Sesión '{command.SessionId}' no encontrada.");

        if (session.Status != SessionStatus.Preparing && session.Status != SessionStatus.Scheduled)
            throw new InvalidOperationException(
                "Solo se pueden crear equipos cuando la sesión está en estado Programada o En Preparación.");

        var isUnique = await _repository.IsTeamNameUniqueInSessionAsync(command.SessionId, command.Name, ct);
        if (!isUnique)
            throw new InvalidOperationException(
                $"Ya existe un equipo con el nombre '{command.Name}' en esta sesión.");

        var team = SessionTeam.Create(command.SessionId, command.Name);
        await _repository.AddTeamAsync(team, ct);

        _logger.LogInformation(
            "Team created: Id={TeamId}, Name={Name}, SessionId={SessionId}",
            team.Id, team.Name, team.SessionId);

        return new CreateSessionTeamResult(team.Id, team.Name, team.MaxMembers, team.CreatedAt);
    }
}
