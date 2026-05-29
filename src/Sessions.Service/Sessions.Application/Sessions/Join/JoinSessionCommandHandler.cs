using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Sessions.Application.Common.Interfaces;
using Sessions.Domain.Enums;

namespace Sessions.Application.Sessions.Join;

public class JoinSessionCommandHandler : IRequestHandler<JoinSessionCommand, JoinSessionCommandResult>
{
    private readonly ISessionRepository _repository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<JoinSessionCommandHandler> _logger;

    public JoinSessionCommandHandler(
        ISessionRepository repository,
        IHttpContextAccessor httpContextAccessor,
        ILogger<JoinSessionCommandHandler> logger)
    {
        _repository = repository;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<JoinSessionCommandResult> Handle(JoinSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _repository.GetByPinAsync(request.Pin, cancellationToken);

        if (session is null)
        {
            throw new InvalidOperationException($"Session with PIN '{request.Pin}' not found");
        }

        if (session.Status != SessionStatus.Preparing)
        {
            throw new InvalidOperationException($"Cannot join session in '{session.Status}' status");
        }

        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst("sub")
            ?? _httpContextAccessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new InvalidOperationException("User identifier not found in token");
        }

        session.AddParticipant(userId);
        await _repository.UpdateAsync(session, cancellationToken);

        var participant = session.Participants.Single(p => p.UserId == userId);

        _logger.LogInformation(
            "User {UserId} joined session {SessionId}",
            userId, session.Id);

        return new JoinSessionCommandResult(session.Id, userId, participant.JoinedAt);
    }
}