using MediatR;
using Sessions.Application.Sessions.Create;

namespace Sessions.Application.Sessions.Create;

public record CreateSessionCommand(
    string Name,
    Guid MissionId,
    string MissionTitle) : IRequest<CreateSessionCommandResult>;
