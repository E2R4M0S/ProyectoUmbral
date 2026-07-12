using MediatR;
using Teams.Application.Teams.Operators.Create;

namespace Teams.Application.Teams.Operators.Create;

public record CreateOperatorCommand(
    string Name,
    string Email) : IRequest<CreateOperatorResult>;
