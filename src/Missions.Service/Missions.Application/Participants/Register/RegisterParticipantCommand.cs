using MediatR;

namespace Missions.Application.Participants.Register;

public record RegisterParticipantCommand(
    string Name,
    string Alias,
    string Email,
    string Password) : IRequest<Guid>;
