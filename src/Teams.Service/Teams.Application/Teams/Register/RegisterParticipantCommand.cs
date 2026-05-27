using MediatR;

namespace Teams.Application.Teams.Register;

public record RegisterParticipantCommand(
    string Name,
    string Alias,
    string Email,
    string Password) : IRequest<Guid>;
