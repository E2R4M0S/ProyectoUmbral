using MediatR;

namespace Teams.Application.Teams.Update;

public record UpdateTeamCommand(
    Guid Id,
    string Name,
    string Description,
    List<string>? AddMemberIds,
    List<string>? RemoveMemberIds) : IRequest;
