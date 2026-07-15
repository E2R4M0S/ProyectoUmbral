using MediatR;

namespace Missions.Application.Participants.Profile;

public record UpdateProfileCommand(
    string FirstName,
    string LastName,
    string Alias,
    string KeycloakUserId) : IRequest<GetProfileResponse>;
