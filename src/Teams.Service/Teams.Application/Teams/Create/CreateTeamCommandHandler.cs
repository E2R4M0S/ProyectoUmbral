using MediatR;
using Microsoft.Extensions.Logging;
using Teams.Application.Common.Interfaces;
using Teams.Application.Teams.Create;
using Teams.Domain.Entities;

namespace Teams.Application.Teams.Create;

public class CreateTeamCommandHandler : IRequestHandler<CreateTeamCommand, CreateTeamCommandResult>
{
    private readonly ITeamRepository _teamRepository;
    private readonly IKeycloakAdminService _keycloakAdminService;
    private readonly ILogger<CreateTeamCommandHandler> _logger;

    public CreateTeamCommandHandler(
        ITeamRepository teamRepository,
        IKeycloakAdminService keycloakAdminService,
        ILogger<CreateTeamCommandHandler> logger)
    {
        _teamRepository = teamRepository;
        _keycloakAdminService = keycloakAdminService;
        _logger = logger;
    }

    public async Task<CreateTeamCommandResult> Handle(CreateTeamCommand command, CancellationToken ct)
    {
        // Validar nombre único
        if (!await _teamRepository.IsNameUniqueAsync(command.Name, ct))
        {
            throw new InvalidOperationException($"Team with name '{command.Name}' already exists.");
        }

        // Validar que el líder existe en Keycloak
        var leader = await _keycloakAdminService.GetUserByIdAsync(command.LeaderId, ct);
        if (leader is null)
        {
            throw new InvalidOperationException($"Leader with ID '{command.LeaderId}' not found in Keycloak.");
        }

        // Validar que todos los miembros existen en Keycloak
        foreach (var memberId in command.MemberIds)
        {
            var member = await _keycloakAdminService.GetUserByIdAsync(memberId, ct);
            if (member is null)
            {
                throw new InvalidOperationException($"Member with ID '{memberId}' not found in Keycloak.");
            }
        }

        // Crear el equipo
        var team = Team.Create(command.Name, command.Description, command.LeaderId);

        // Agregar los miembros
        foreach (var memberId in command.MemberIds)
        {
            team.AddMember(memberId);
        }

        // Persistir
        await _teamRepository.AddAsync(team, ct);

        _logger.LogInformation(
            "Team created: Id={TeamId}, Name={TeamName}, LeaderId={LeaderId}",
            team.Id, team.Name, team.LeaderId);

        return new CreateTeamCommandResult(
            team.Id,
            team.Name,
            team.Description,
            team.LeaderId,
            team.Members.Select(m => m.UserId).ToList(),
            team.CreatedAt);
    }
}
