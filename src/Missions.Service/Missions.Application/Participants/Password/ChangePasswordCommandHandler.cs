using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Missions.Application.Common.Interfaces;

namespace Missions.Application.Participants.Password;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand>
{
    private readonly IKeycloakAdminService _keycloakAdminService;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(
        IKeycloakAdminService keycloakAdminService,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _keycloakAdminService = keycloakAdminService;
        _logger = logger;
    }

    public async Task Handle(ChangePasswordCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.NewPassword))
        {
            _logger.LogWarning(
                "Password change rejected: new password is empty for KeycloakUserId={KeycloakUserId}",
                command.KeycloakUserId);
            throw new ValidationException("New password is required.");
        }

        await _keycloakAdminService.VerifyPasswordAsync(command.Email, command.CurrentPassword, ct);
        await _keycloakAdminService.ResetPasswordAsync(command.KeycloakUserId, command.NewPassword, ct);

        _logger.LogInformation(
            "Password changed for KeycloakUserId={KeycloakUserId}", command.KeycloakUserId);
    }
}
