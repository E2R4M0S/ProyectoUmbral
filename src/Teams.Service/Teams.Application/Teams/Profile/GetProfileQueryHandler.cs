using MediatR;
using Teams.Application.Common.Interfaces;
using Teams.Domain.Entities;

namespace Teams.Application.Teams.Profile;

public class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, GetProfileResponse?>
{
    private readonly IParticipantRepository _participantRepository;
    private readonly IKeycloakAdminService _keycloakAdmin;

    public GetProfileQueryHandler(
        IParticipantRepository participantRepository,
        IKeycloakAdminService keycloakAdmin)
    {
        _participantRepository = participantRepository;
        _keycloakAdmin = keycloakAdmin;
    }

    public async Task<GetProfileResponse?> Handle(GetProfileQuery query, CancellationToken ct)
    {
        var participant = await _participantRepository.GetByKeycloakUserIdAsync(query.KeycloakUserId, ct);

        if (participant is null)
        {
            var keycloakUser = await _keycloakAdmin.GetUserByIdAsync(query.KeycloakUserId, ct);
            if (keycloakUser is null)
                return null;

            var alias = "participante";
            if (keycloakUser.Attributes != null && keycloakUser.Attributes.TryGetValue("alias", out var attrs) && attrs.Length > 0)
                alias = attrs[0];
            else if (!string.IsNullOrEmpty(keycloakUser.Email))
                alias = keycloakUser.Email.Split('@')[0];

            participant = Participant.Create(
                keycloakUser.FirstName ?? "Participante",
                "",
                keycloakUser.Email?.Split('@')[0] ?? alias,
                alias,
                string.IsNullOrEmpty(keycloakUser.Email) ? $"{alias}@umbral.local" : keycloakUser.Email,
                query.KeycloakUserId);

            await _participantRepository.AddAsync(participant, ct);
        }

        return new GetProfileResponse(participant.FirstName, participant.LastName, participant.Alias, participant.Email);
    }
}