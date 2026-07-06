# Skill: CQRS con MediatR — UMBRAL

## Cuándo aplicar este skill
Cuando necesites implementar cualquier operación en un microservicio de UMBRAL: crear, modificar, consultar o eliminar algo. Toda operación pasa por un Command o Query de MediatR.

## Estructura de archivos para un Command

```
src/<Servicio>.Service/
  <Servicio>.Application/
    Commands/
      <Acción><Entidad>/                    ← una carpeta por operación
        <Acción><Entidad>Command.cs
        <Acción><Entidad>CommandHandler.cs
        <Acción><Entidad>CommandValidator.cs
        <Acción><Entidad>CommandResult.cs   ← opcional si solo retorna void
```

## Template completo: Command

```csharp
// 1. Command (record inmutable)
public record CreateStageCommand(
    Guid MissionId,
    string Name,
    string Description,
    int Order
) : IRequest<CreateStageCommandResult>;

// 2. Result
public record CreateStageCommandResult(Guid StageId);

// 3. Validator
public class CreateStageCommandValidator : AbstractValidator<CreateStageCommand>
{
    public CreateStageCommandValidator()
    {
        RuleFor(x => x.MissionId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Order).GreaterThan(0);
    }
}

// 4. Handler
public class CreateStageCommandHandler : IRequestHandler<CreateStageCommand, CreateStageCommandResult>
{
    private readonly IMissionRepository _repo;

    public CreateStageCommandHandler(IMissionRepository repo) => _repo = repo;

    public async Task<CreateStageCommandResult> Handle(
        CreateStageCommand command,
        CancellationToken cancellationToken)
    {
        var mission = await _repo.GetByIdAsync(command.MissionId, cancellationToken)
            ?? throw new NotFoundException($"Mission {command.MissionId} not found");

        mission.AddStage(command.Name, command.Description, command.Order);

        await _repo.UpdateAsync(mission, cancellationToken);

        return new CreateStageCommandResult(mission.Stages.Last().Id);
    }
}
```

## Template completo: Query

```csharp
// Query
public record GetMissionByIdQuery(Guid MissionId) : IRequest<MissionDetailDto?>;

// DTO de respuesta
public record MissionDetailDto(
    Guid Id,
    string Title,
    string Description,
    string Difficulty,
    int TimeMinutes,
    string Type,
    string Status,
    List<StageDto> Stages
);

// Handler — puede ir directo al DbContext para lecturas (no necesita repositorio)
public class GetMissionByIdQueryHandler : IRequestHandler<GetMissionByIdQuery, MissionDetailDto?>
{
    private readonly MissionsDbContext _db;

    public GetMissionByIdQueryHandler(MissionsDbContext db) => _db = db;

    public async Task<MissionDetailDto?> Handle(
        GetMissionByIdQuery query,
        CancellationToken cancellationToken)
    {
        var mission = await _db.Missions
            .Include(m => m.Stages)
            .ThenInclude(s => s.Clues)
            .FirstOrDefaultAsync(m => m.Id == query.MissionId, cancellationToken);

        if (mission is null) return null;

        return new MissionDetailDto(
            mission.Id,
            mission.Title,
            mission.Description,
            mission.Difficulty.ToString(),
            mission.TimeMinutes,
            mission.Type.ToString(),
            mission.Status.ToString(),
            mission.Stages.Select(s => new StageDto(s.Id, s.Name, s.Order)).ToList()
        );
    }
}
```

## Endpoint que invoca el Command/Query

```csharp
// En <Servicio>.Api/Endpoints/<Entidad>Endpoints.cs
public static class MissionEndpoints
{
    public static void MapMissionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/missions");

        group.MapPost("/", async (CreateMissionCommand command, IMediator mediator) =>
        {
            var result = await mediator.Send(command);
            return Results.Created($"/missions/{result.MissionId}", result);
        })
        .RequireAuthorization("admin");

        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetMissionByIdQuery(id));
            return result is null ? Results.NotFound() : Results.Ok(result);
        });
    }
}
```

## Registro en DependencyInjection

```csharp
// En <Servicio>.Application/DependencyInjection.cs
public static IServiceCollection AddApplication(this IServiceCollection services)
{
    services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

    services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

    // Pipeline de validación automática
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>));

    return services;
}
```

## Reglas CQRS que no se rompen
- Commands pueden escribir en DB. Nunca retornan datos de lectura complejos (máximo el ID creado).
- Queries solo leen. Nunca modifican estado.
- Los Handlers no contienen lógica de negocio compleja — esa va en las entidades de dominio.
- Un Handler por Command/Query. No reutilices handlers.
