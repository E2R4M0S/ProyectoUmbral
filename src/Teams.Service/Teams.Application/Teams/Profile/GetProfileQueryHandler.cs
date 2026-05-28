using MediatR;
using Teams.Application.Common.Interfaces;

namespace Teams.Application.Teams.Profile;

public class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, GetProfileResponse?>
{
    private readonly IParticipantRepository _participantRepository;

    public GetProfileQueryHandler(IParticipantRepository participantRepository)
    {
        _participantRepository = participantRepository;
    }

    public async Task<GetProfileResponse?> Handle(GetProfileQuery query, CancellationToken ct)
    {
        var participant = await _participantRepository.GetByKeycloakUserIdAsync(query.KeycloakUserId, ct);

        if (participant is null)
            return null;

        return new GetProfileResponse(participant.Name, participant.Alias, participant.Email);
    }
}