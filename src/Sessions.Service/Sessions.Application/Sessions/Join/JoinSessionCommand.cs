using MediatR;

namespace Sessions.Application.Sessions.Join;

public record JoinSessionCommand(string Pin) : IRequest<JoinSessionCommandResult>;