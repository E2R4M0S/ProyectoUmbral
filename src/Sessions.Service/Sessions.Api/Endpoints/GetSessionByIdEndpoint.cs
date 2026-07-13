using MediatR;
using Sessions.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Sessions.Api.Endpoints;

public static class GetSessionByIdEndpoint
{
    public static void MapGetSessionByIdEndpoint(this WebApplication app)
    {
        app.MapGet("/{id:guid}", async (
            [FromRoute] Guid id,
            ISessionRepository repository,
            ILogger<Program> logger) =>
        {
            var session = await repository.GetByIdWithStagesAsync(id, CancellationToken.None);
            if (session is null)
                return Results.NotFound(new { error = "Session not found" });

            return Results.Ok(new
            {
                session.Id,
                session.Name,
                session.Pin,
                session.CurrentStageOrder,
                Stages = session.Stages.Select(s => new
                {
                    s.MissionId,
                    s.MissionTitle,
                    s.StageName,
                    s.MissionType,
                    s.Order,
                    s.MissionStageId,
                    s.QrToken,
                    s.TimeMinutes,
                    QuizId = s.MissionType == "Trivia" ? s.MissionStageId : (Guid?)null,
                }),
                Status = session.Status.ToString(),
                session.StartedAt,
                session.EndedAt,
                session.CreatedAt,
                Participants = session.Participants.Select(p => new
                {
                    p.UserId,
                    p.JoinedAt
                })
            });
        })
        .WithName("GetSessionById")
        .RequireAuthorization();
    }
}
