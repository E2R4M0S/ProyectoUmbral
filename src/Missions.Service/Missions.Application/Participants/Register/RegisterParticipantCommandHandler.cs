using MediatR;
using Microsoft.Extensions.Logging;
using Missions.Application.Common.Exceptions;
using Missions.Application.Common.Interfaces;
using Missions.Domain.Entities;

namespace Missions.Application.Participants.Register;

public class RegisterParticipantCommandHandler : IRequestHandler<RegisterParticipantCommand, Guid>
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
        var aliasUnique = await _participantRepository.IsAliasUniqueAsync(command.Alias, ct);
        if (!aliasUnique)
        {
            _logger.LogWarning("Registration failed — alias '{Alias}' is already taken", command.Alias);
            throw new RegistrationException("Alias", $"Alias '{command.Alias}' is already taken.");
        }

        var emailUnique = await _participantRepository.IsEmailUniqueAsync(command.Email, ct);
        if (!emailUnique)
        {
            _logger.LogWarning("Registration failed — email '{Email}' is already registered", command.Email);
            throw new RegistrationException("Email", $"Email '{command.Email}' is already registered.");
        }

        string keycloakUserId;
        try
        {
            keycloakUserId = await _keycloakAdminService.CreateUserAsync(
                command.Username,
                command.Email,
                command.Password,
                command.FirstName,
                command.LastName,
                command.Alias,
                ct);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already registered"))
        {
            _logger.LogWarning("Registration failed — email '{Email}' already exists in Keycloak", command.Email);
            throw new RegistrationException("Email", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create user in Keycloak for email '{Email}'", command.Email);
            throw;
        }

        try
        {
            await _keycloakAdminService.ExecuteActionsEmailAsync(keycloakUserId, ["VERIFY_EMAIL"], ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send verification email to Keycloak user {KeycloakUserId}", keycloakUserId);
        }

        Participant participant;
        try
        {
            participant = Participant.Create(
                command.FirstName,
                command.LastName,
                command.Username,
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
