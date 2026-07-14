using MediatR;

namespace Sessions.Application.Sessions.Teams.RemoveMember;

public record RemoveTeamMemberCommand(Guid SessionId, Guid TeamId, Guid UserId) : IRequest;
