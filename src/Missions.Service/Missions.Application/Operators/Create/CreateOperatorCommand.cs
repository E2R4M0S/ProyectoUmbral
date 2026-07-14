using MediatR;

namespace Missions.Application.Operators.Create;

public record CreateOperatorCommand(
    string Name,
    string Email) : IRequest<CreateOperatorResult>;
