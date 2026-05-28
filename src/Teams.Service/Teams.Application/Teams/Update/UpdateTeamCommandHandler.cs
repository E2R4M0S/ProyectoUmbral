using MediatR;
using Microsoft.Extensions.Logging;
using Teams.Application.Common.Interfaces;
using Teams.Application.Teams.Update;

namespace Teams.Application.Teams.Update;

public class UpdateTeamCommandHandler : IRequestHandler<UpdateTeamCommand>
{
    private readonly ITeamRepository _teamRepository;
    private readonly IKeycloakAdminService _keycloakAdminService;
    private readonly ILogger<UpdateTeamCommandHandler> _logger;

    public UpdateTeamCommandHandler(
        ITeamRepository teamRepository,
        IKeycloakAdminService keycloakAdminService,
        ILogger<UpdateTeamCommandHandler> logger)
    {
        _teamRepository = teamRepository;
        _keycloakAdminService = keycloakAdminService;
        _logger = logger;
    }

    public async Task Handle(UpdateTeamCommand command, CancellationToken ct)
    {
        var team = await _teamRepository.GetByIdWithMembersAsync(command.Id, ct);
        if (team is null)
        {
            throw new InvalidOperationException($"Team with id '{command.Id}' not found");
        }

        team.Update(command.Name, command.Description);

        if (command.AddMemberIds is not null)
        {
            foreach (var memberId in command.AddMemberIds)
            {
                var user = await _keycloakAdminService.GetUserByIdAsync(memberId, ct);
                if (user is null)
                {
                    throw new InvalidOperationException($"User with ID '{memberId}' not found in Keycloak.");
                }
                team.AddMember(memberId);
            }
        }

        if (command.RemoveMemberIds is not null)
        {
            foreach (var memberId in command.RemoveMemberIds)
            {
                team.RemoveMember(memberId);
            }
        }

        await _teamRepository.UpdateAsync(team, ct);

        _logger.LogInformation(
            "Team updated: Id={TeamId}, Name={TeamName}",
            team.Id, team.Name);
    }
}
