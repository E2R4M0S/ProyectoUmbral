using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Sessions.Application.Common.Interfaces;
using Sessions.Domain.Enums;

namespace Sessions.Application.Sessions.Teams.JoinTeam;

public class JoinSessionTeamCommandHandler
    : IRequestHandler<JoinSessionTeamCommand, JoinSessionTeamResult>
{
    private readonly ISessionRepository _repository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<JoinSessionTeamCommandHandler> _logger;

    public JoinSessionTeamCommandHandler(
        ISessionRepository repository,
        IHttpContextAccessor httpContextAccessor,
        ILogger<JoinSessionTeamCommandHandler> logger)
    {
        _repository = repository;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<JoinSessionTeamResult> Handle(
        JoinSessionTeamCommand command,
        CancellationToken ct)
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst("sub")
            ?? _httpContextAccessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
            throw new InvalidOperationException("Identificador de usuario no encontrado en el token.");

        var userAlias = _httpContextAccessor.HttpContext?.User.FindFirst("alias")?.Value
            ?? _httpContextAccessor.HttpContext?.User.FindFirst("preferred_username")?.Value
            ?? _httpContextAccessor.HttpContext?.User.FindFirst("name")?.Value;

        var session = await _repository.GetByIdAsync(command.SessionId, ct)
            ?? throw new InvalidOperationException($"Sesión '{command.SessionId}' no encontrada.");

        if (session.Status != SessionStatus.Preparing)
            throw new InvalidOperationException(
                "Solo se puede unir a un equipo cuando la sesión está En Preparación.");

        var participant = await _repository.GetParticipantAsync(command.SessionId, userId, ct);
        if (participant is null)
            throw new InvalidOperationException(
                "Debes unirte a la sesión antes de unirte a un equipo.");

        var currentTeam = await _repository.GetParticipantTeamInSessionAsync(command.SessionId, userId, ct);
        if (currentTeam is not null)
        {
            if (currentTeam.Id == command.TeamId)
                return new JoinSessionTeamResult(currentTeam.Id, currentTeam.Name, userId, userAlias ?? userId.ToString("N")[..8]);

            throw new InvalidOperationException(
                $"Ya eres miembro del equipo '{currentTeam.Name}'. Debes salir de ese equipo primero.");
        }

        var team = await _repository.GetTeamByIdAsync(command.TeamId, ct)
            ?? throw new InvalidOperationException($"Equipo '{command.TeamId}' no encontrado.");

        if (team.SessionId != command.SessionId)
            throw new InvalidOperationException("El equipo no pertenece a esta sesión.");

        team.AddMember(userId, userAlias);
        await _repository.UpdateTeamAsync(team, ct);

        _logger.LogInformation(
            "User {UserId} joined team {TeamId} ({TeamName}) in session {SessionId}",
            userId, team.Id, team.Name, command.SessionId);

        var alias = team.Members.First(m => m.UserId == userId).UserAlias;
        return new JoinSessionTeamResult(team.Id, team.Name, userId, alias);
    }
}
