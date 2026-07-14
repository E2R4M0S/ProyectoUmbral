using MediatR;
using Microsoft.Extensions.Logging;
using Missions.Application.Common.Interfaces;

namespace Missions.Application.Operators.Disable;

public class DisableOperatorCommandHandler : IRequestHandler<DisableOperatorCommand, DisableOperatorResponse>
{
    private readonly IKeycloakAdminService _keycloakAdminService;
    private readonly ILogger<DisableOperatorCommandHandler> _logger;

    public DisableOperatorCommandHandler(
        IKeycloakAdminService keycloakAdminService,
        ILogger<DisableOperatorCommandHandler> logger)
    {
        _keycloakAdminService = keycloakAdminService;
        _logger = logger;
    }

    public async Task<DisableOperatorResponse> Handle(DisableOperatorCommand command, CancellationToken ct)
    {
        try
        {
            var result = await _keycloakAdminService.DisableOperatorAsync(command.Email, ct);

            _logger.LogInformation(
                "Operator disabled: Email={Email}, WasAlreadyDisabled={WasAlreadyDisabled}",
                command.Email, result.WasAlreadyDisabled);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to disable operator in Keycloak: Email={Email}", command.Email);
            throw;
        }
    }
}
