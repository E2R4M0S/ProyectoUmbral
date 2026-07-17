using MediatR;
using Microsoft.Extensions.Logging;
using Missions.Application.Common.Interfaces;

namespace Missions.Application.Operators.Enable;

public class EnableOperatorCommandHandler : IRequestHandler<EnableOperatorCommand, EnableOperatorResponse>
{
    private readonly IKeycloakAdminService _keycloakAdminService;
    private readonly ILogger<EnableOperatorCommandHandler> _logger;

    public EnableOperatorCommandHandler(
        IKeycloakAdminService keycloakAdminService,
        ILogger<EnableOperatorCommandHandler> logger)
    {
        _keycloakAdminService = keycloakAdminService;
        _logger = logger;
    }

    public async Task<EnableOperatorResponse> Handle(EnableOperatorCommand command, CancellationToken ct)
    {
        try
        {
            var result = await _keycloakAdminService.EnableOperatorAsync(command.Email, ct);

            _logger.LogInformation(
                "Operator enabled: Email={Email}, WasAlreadyEnabled={WasAlreadyEnabled}",
                command.Email, result.WasAlreadyEnabled);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to enable operator in Keycloak: Email={Email}", command.Email);
            throw;
        }
    }
}
