using MediatR;
using Microsoft.Extensions.Logging;
using Teams.Application.Common.Exceptions;
using Teams.Application.Common.Interfaces;
using Teams.Domain.Entities;

namespace Teams.Application.Teams.Profile;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, GetProfileResponse>
{
    private readonly IKeycloakAdminService _keycloakAdminService;
    private readonly IParticipantRepository _participantRepository;
    private readonly ILogger<UpdateProfileCommandHandler> _logger;

    public UpdateProfileCommandHandler(
        IKeycloakAdminService keycloakAdminService,
        IParticipantRepository participantRepository,
        ILogger<UpdateProfileCommandHandler> logger)
    {
        _keycloakAdminService = keycloakAdminService;
        _participantRepository = participantRepository;
        _logger = logger;
    }

    public async Task<GetProfileResponse> Handle(UpdateProfileCommand command, CancellationToken ct)
    {
        var participant = await _participantRepository.GetByKeycloakUserIdAsync(command.KeycloakUserId, ct);

        if (participant is null)
            throw new InvalidOperationException($"Participant not found for KeycloakUserId: {command.KeycloakUserId}");

        // Check alias uniqueness (exclude self)
        var aliasUnique = await _participantRepository.IsAliasUniqueAsync(command.Alias, command.KeycloakUserId, ct);
        if (!aliasUnique)
        {
            _logger.LogWarning(
                "Profile update failed — alias '{Alias}' is already taken by another participant",
                command.Alias);
            throw new RegistrationException("Alias", $"Alias '{command.Alias}' is already taken.");
        }

        // Update Keycloak first (dual-write pattern)
        await _keycloakAdminService.UpdateUserAsync(command.KeycloakUserId, command.FirstName, command.LastName, command.Alias, ct);

        // Update local DB second
        try
        {
            participant.Update(command.FirstName, command.LastName, command.Alias);
            await _participantRepository.UpdateAsync(participant, ct);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex,
                "Profile update failed: Keycloak updated but DB update failed for KeycloakUserId={KeycloakUserId}. " +
                "Attempted values: FirstName={FirstName}, LastName={LastName}, Alias={Alias}",
                command.KeycloakUserId, command.FirstName, command.LastName, command.Alias);
            throw;
        }

        _logger.LogInformation(
            "Profile updated: KeycloakUserId={KeycloakUserId}, Alias={Alias}",
            command.KeycloakUserId, command.Alias);

        return new GetProfileResponse(participant.FirstName, participant.LastName, participant.Alias, participant.Email);
    }
}