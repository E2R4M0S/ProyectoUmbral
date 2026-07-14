using MediatR;

namespace Missions.Application.Operators.Disable;

public record DisableOperatorCommand(string Email) : IRequest<DisableOperatorResponse>;
