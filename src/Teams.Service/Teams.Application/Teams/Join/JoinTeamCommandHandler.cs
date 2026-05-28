using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Teams.Application.Common.Interfaces;
using Teams.Application.Teams.Join;

namespace Teams.Application.Teams.Join;

public class JoinTeamCommandHandler : IRequestHandler<JoinTeamCommand, JoinTeamCommandResult>
{
    private readonly ITeamRepository _teamRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<JoinTeamCommandHandler> _logger;

    public JoinTeamCommandHandler(
        ITeamRepository teamRepository,
        IHttpContextAccessor httpContextAccessor,
        ILogger<JoinTeamCommandHandler> logger)
    {
        _teamRepository = teamRepository;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<JoinTeamCommandResult> Handle(JoinTeamCommand command, CancellationToken ct)
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new InvalidOperationException("User ID not found in claims");
        }

        var team = await _teamRepository.GetByIdWithMembersAsync(command.TeamId, ct);
        if (team is null)
        {
            throw new InvalidOperationException($"Team with id '{command.TeamId}' not found");
        }

        if (team.JoinCode != command.JoinCode)
        {
            throw new InvalidOperationException("Invalid join code");
        }

        if (team.Members.Any(m => m.UserId == userId))
        {
            throw new InvalidOperationException($"User '{userId}' is already a member");
        }

        team.AddMember(userId);
        await _teamRepository.UpdateAsync(team, ct);

        _logger.LogInformation(
            "User {UserId} joined team: TeamId={TeamId}",
            userId, team.Id);

        return JoinTeamCommandResult.Success(team.Id);
    }
}
