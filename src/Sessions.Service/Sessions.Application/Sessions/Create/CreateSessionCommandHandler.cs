using MediatR;
using Microsoft.Extensions.Logging;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.Create;
using Sessions.Domain.Entities;

namespace Sessions.Application.Sessions.Create;

public class CreateSessionCommandHandler
    : IRequestHandler<CreateSessionCommand, CreateSessionCommandResult>
{
    private readonly ISessionRepository _repository;
    private readonly ILogger<CreateSessionCommandHandler> _logger;

    public CreateSessionCommandHandler(
        ISessionRepository repository,
        ILogger<CreateSessionCommandHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<CreateSessionCommandResult> Handle(
        CreateSessionCommand command,
        CancellationToken ct)
    {
        var existing = await _repository.GetByNameAsync(command.Name, ct);
        if (existing is not null)
        {
            throw new InvalidOperationException($"Ya existe una sesión con el nombre '{command.Name}'");
        }

        var pin = await GenerateUniquePinAsync(ct);

        var stages = command.Stages
            .Select(s => SessionStage.Create(
                s.MissionId,
                s.MissionStageId,
                s.MissionTitle,
                s.MissionType,
                s.Order,
                s.QrToken,
                s.Latitude,
                s.Longitude))
            .ToList();

        var session = Session.Create(command.Name, pin, stages);

        await _repository.AddAsync(session, ct);

        _logger.LogInformation(
            "Session created: Id={SessionId}, Name={Name}, Pin={Pin}, StageCount={StageCount}",
            session.Id, session.Name, session.Pin, session.Stages.Count);

        return new CreateSessionCommandResult(
            session.Id,
            session.Name,
            session.Pin,
            session.Status.ToString(),
            session.CurrentStageOrder,
            session.Stages.Select(s => new StageOutput(s.MissionId, s.MissionTitle, s.MissionType, s.Order)).ToList(),
            session.StartedAt,
            session.EndedAt,
            session.CreatedAt);
    }

    private static string GeneratePin()
        => Random.Shared.Next(0, 1_000_000).ToString("D6");

    private async Task<string> GenerateUniquePinAsync(CancellationToken ct)
    {
        for (int i = 0; i < 10; i++)
        {
            var pin = GeneratePin();
            if (await _repository.IsPinUniqueAsync(pin, ct))
                return pin;
        }
        throw new InvalidOperationException(
            "Could not generate unique PIN after 10 attempts");
    }
}
