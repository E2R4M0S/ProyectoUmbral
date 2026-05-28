using MediatR;
using Teams.Application.Teams.Operators.Disable;

namespace Teams.Application.Teams.Operators.Disable;

public record DisableOperatorCommand(string Email) : IRequest<DisableOperatorResponse>;