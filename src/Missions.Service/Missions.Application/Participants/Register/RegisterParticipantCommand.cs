using MediatR;

namespace Missions.Application.Participants.Register;

public record RegisterParticipantCommand(
    string FirstName,
    string LastName,
    string Username,
    string Alias,
    string Email,
    string Password) : IRequest<Guid>;
