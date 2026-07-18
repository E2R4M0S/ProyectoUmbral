using MediatR;

namespace Missions.Application.Participants.Password;

public record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword,
    string KeycloakUserId,
    string Email) : IRequest;
