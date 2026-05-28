using MediatR;
using Sessions.Application.Sessions.Create;

namespace Sessions.Application.Sessions.Create;

public record CreateSessionCommand(
    string Name,
    Guid MissionId) : IRequest<CreateSessionCommandResult>;
