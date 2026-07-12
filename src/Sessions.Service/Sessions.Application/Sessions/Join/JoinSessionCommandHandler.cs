using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Sessions.Application.Common.Interfaces;
using Sessions.Domain.Entities;
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

        var canJoin = session.Status == SessionStatus.Preparing
            || session.Status == SessionStatus.Active
            || session.Status == SessionStatus.Paused;

        if (!canJoin)
        {
            throw new InvalidOperationException($"Cannot join session in '{session.Status}' status");
        }

        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst("sub")
            ?? _httpContextAccessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new InvalidOperationException("User identifier not found in token");
        }

        // Idempotent: if participant already exists, return their data without inserting
        var existing = await _repository.GetParticipantAsync(session.Id, userId, cancellationToken);
        if (existing is not null)
        {
            _logger.LogInformation(
                "User {UserId} reconnected to session {SessionId}",
                userId, session.Id);
            return new JoinSessionCommandResult(session.Id, userId, existing.JoinedAt);
        }

        var userAlias = _httpContextAccessor.HttpContext?.User.FindFirst("alias")?.Value
            ?? _httpContextAccessor.HttpContext?.User.FindFirst("preferred_username")?.Value
            ?? _httpContextAccessor.HttpContext?.User.FindFirst("name")?.Value;

        var newParticipant = SessionParticipant.Create(session.Id, userId, userAlias);
        await _repository.AddParticipantAsync(newParticipant, cancellationToken);

        _logger.LogInformation(
            "User {UserId} joined session {SessionId}",
            userId, session.Id);

        return new JoinSessionCommandResult(session.Id, userId, newParticipant.JoinedAt);
    }
}