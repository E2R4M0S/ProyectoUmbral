using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Sessions.Application.Common;
using Sessions.Application.Common.Interfaces;
using Sessions.Application.Sessions.Create;
using Sessions.Domain.Entities;

namespace Sessions.Application.Sessions.Create;

public class CreateSessionCommandHandler
    : IRequestHandler<CreateSessionCommand, CreateSessionCommandResult>
{
    private readonly ISessionRepository _repository;
    private readonly IMissionCatalogService _missionCatalogService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CreateSessionCommandHandler> _logger;

    public CreateSessionCommandHandler(
        ISessionRepository repository,
        IMissionCatalogService missionCatalogService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<CreateSessionCommandHandler> logger)
    {
        _repository = repository;
        _missionCatalogService = missionCatalogService;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<CreateSessionCommandResult> Handle(
        CreateSessionCommand command,
        CancellationToken ct)
    {
        // RB-10: the creating operator becomes the only one who can manage this session.
        var operatorId = CurrentUserClaims.GetUserId(_httpContextAccessor.HttpContext?.User);
        if (operatorId is null)
        {
            throw new InvalidOperationException("Operator identifier not found in token");
        }

        var existing = await _repository.GetByNameAsync(command.Name, ct);
        if (existing is not null)
        {
            throw new InvalidOperationException($"Ya existe una sesión con el nombre '{command.Name}'");
        }

        // RB-01: una misión solo puede usarse en sesiones si está en estado Activa.
        var missionsById = await EnsureMissionsAreActiveAsync(command.Stages, ct);

        var pin = await GenerateUniquePinAsync(ct);

        var stages = command.Stages
            .Select(s => SessionStage.Create(
                s.MissionId,
                s.MissionStageId,
                s.MissionTitle,
                s.StageName,
                s.MissionType,
                s.Order,
                s.QrToken,
                s.TimeMinutes,
                s.Latitude,
                s.Longitude,
                missionsById.TryGetValue(s.MissionId, out var mission) ? mission.Difficulty : "Medium"))
            .ToList();

        var session = Session.Create(command.Name, pin, stages, operatorId);

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

    private async Task<Dictionary<Guid, MissionSummary>> EnsureMissionsAreActiveAsync(List<StageInput> stages, CancellationToken ct)
    {
        var failures = new List<ValidationFailure>();
        var missionsById = new Dictionary<Guid, MissionSummary>();

        foreach (var missionId in stages.Select(s => s.MissionId).Distinct())
        {
            var mission = await _missionCatalogService.GetMissionAsync(missionId, ct);
            if (mission is null)
            {
                failures.Add(new ValidationFailure(
                    nameof(CreateSessionCommand.Stages),
                    $"La misión '{missionId}' no existe o no pudo ser verificada."));
                continue;
            }

            missionsById[missionId] = mission;

            if (mission.Status != "Active")
            {
                failures.Add(new ValidationFailure(
                    nameof(CreateSessionCommand.Stages),
                    $"La misión '{mission.Title}' no está Activa (estado actual: {mission.Status}) y no puede usarse en una sesión."));
            }
        }

        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return missionsById;
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
