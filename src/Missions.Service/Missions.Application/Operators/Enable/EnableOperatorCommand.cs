using MediatR;

namespace Missions.Application.Operators.Enable;

public record EnableOperatorCommand(string Email) : IRequest<EnableOperatorResponse>;
