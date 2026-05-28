using MediatR;

namespace Teams.Application.Teams.Profile;

public record GetProfileQuery(string KeycloakUserId) : IRequest<GetProfileResponse?>;