using MediatR;
using Microsoft.Extensions.Logging;
using Teams.Application.Common.Interfaces;
using Teams.Application.Teams.Operators.Create;

namespace Teams.Application.Teams.Operators.Create;

public class CreateOperatorCommandHandler
    : IRequestHandler<CreateOperatorCommand, CreateOperatorResult>
{
    private readonly IKeycloakAdminService _keycloakAdminService;
    private readonly ILogger<CreateOperatorCommandHandler> _logger;

    public CreateOperatorCommandHandler(
        IKeycloakAdminService keycloakAdminService,
        ILogger<CreateOperatorCommandHandler> logger)
    {
        _keycloakAdminService = keycloakAdminService;
        _logger = logger;
    }

    public async Task<CreateOperatorResult> Handle(CreateOperatorCommand command, CancellationToken ct)
    {
        try
        {
            var result = await _keycloakAdminService.CreateOperatorAsync(
                command.Name,
                command.Email,
                ct);

            _logger.LogInformation(
                "Operator created: Name={Name}, Email={Email}, KeycloakUserId={KeycloakUserId}",
                result.Name, result.Email, result.KeycloakUserId);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to create operator in Keycloak: Email={Email}", command.Email);
            throw;
        }
    }
}
