using MediatR;

namespace Teams.Application.Teams.Profile;

public record UpdateProfileCommand(
    string Name,
    string Alias,
    string KeycloakUserId) : IRequest<GetProfileResponse>;