using MediatR;

namespace Teams.Application.Teams.Register;

public record RegisterParticipantCommand(
    string FirstName,
    string LastName,
    string Username,
    string Alias,
    string Email,
    string Password) : IRequest<Guid>;
