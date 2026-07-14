using MediatR;

namespace Teams.Application.Teams.Profile;

public record UpdateProfileCommand(
    string FirstName,
    string LastName,
    string Alias,
    string KeycloakUserId) : IRequest<GetProfileResponse>;