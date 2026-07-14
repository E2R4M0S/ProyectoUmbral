using MediatR;

namespace Missions.Application.Participants.Profile;

public record GetProfileQuery(string KeycloakUserId) : IRequest<GetProfileResponse?>;
