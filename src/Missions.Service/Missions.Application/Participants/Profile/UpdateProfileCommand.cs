using MediatR;

namespace Missions.Application.Participants.Profile;

public record UpdateProfileCommand(
    string Name,
    string Alias,
    string KeycloakUserId) : IRequest<GetProfileResponse>;
