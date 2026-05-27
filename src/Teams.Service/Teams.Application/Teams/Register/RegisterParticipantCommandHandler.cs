using MediatR;
using Microsoft.Extensions.Logging;
using Teams.Application.Common.Exceptions;
using Teams.Application.Common.Interfaces;
using Teams.Domain.Entities;

namespace Teams.Application.Teams.Register;

public class RegisterParticipantCommandHandler
    : IRequestHandler<RegisterParticipantCommand, Guid>
{
    private readonly IKeycloakAdminService _keycloakAdminService;
    private readonly IParticipantRepository _participantRepository;
    private readonly ILogger<RegisterParticipantCommandHandler> _logger;

    public RegisterParticipantCommandHandler(
        IKeycloakAdminService keycloakAdminService,
        IParticipantRepository participantRepository,
        ILogger<RegisterParticipantCommandHandler> logger)
    {
        _keycloakAdminService = keycloakAdminService;
        _participantRepository = participantRepository;
        _logger = logger;
    }

    public async Task<Guid> Handle(RegisterParticipantCommand command, CancellationToken ct)
    {
        // Check alias uniqueness
        var aliasUnique = await _participantRepository.IsAliasUniqueAsync(command.Alias, ct);
        if (!aliasUnique)
        {
            _logger.LogWarning("Registration failed — alias '{Alias}' is already taken", command.Alias);
            throw new RegistrationException("Alias", $"Alias '{command.Alias}' is already taken.");
        }

        // Check email uniqueness
        var emailUnique = await _participantRepository.IsEmailUniqueAsync(command.Email, ct);
        if (!emailUnique)
        {
            _logger.LogWarning("Registration failed — email '{Email}' is already registered", command.Email);
            throw new RegistrationException("Email", $"Email '{command.Email}' is already registered.");
        }

        // Create user in Keycloak + assign role
        string keycloakUserId;
        try
        {
            keycloakUserId = await _keycloakAdminService.CreateUserAsync(
                command.Email,  // username = email
                command.Email,
                command.Password,
                command.Alias,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user in Keycloak for email '{Email}'", command.Email);
            throw;
        }

        // Persist participant locally with compensating transaction
        Participant participant;
        try
        {
            participant = Participant.Create(
                command.Name,
                command.Alias,
                command.Email,
                keycloakUserId);

            await _participantRepository.AddAsync(participant, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to persist participant after Keycloak user creation — attempting cleanup of Keycloak user {KeycloakUserId}",
                keycloakUserId);

            try
            {
                await _keycloakAdminService.DeleteUserAsync(keycloakUserId, ct);
            }
            catch (Exception cleanupEx)
            {
                _logger.LogError(cleanupEx,
                    "Failed to clean up Keycloak user {KeycloakUserId} after DB failure — manual intervention required",
                    keycloakUserId);
            }

            throw;
        }

        _logger.LogInformation(
            "Participant registered: Id={ParticipantId}, Alias={Alias}, Email={Email}",
            participant.Id, participant.Alias, participant.Email);

        return participant.Id;
    }
}
