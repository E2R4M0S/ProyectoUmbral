# Skill: DDD Modeling — UMBRAL

## Cuándo aplicar este skill
Cuando necesites crear o modificar entidades de dominio, value objects, o definir los límites entre bounded contexts en UMBRAL.

## Bounded Contexts del proyecto

```
┌─────────────────────────┐  ┌─────────────────────────┐
│   Missions BC           │  │   Sessions BC           │
│   Missions.Service      │  │   Sessions.Service      │
│                         │  │                         │
│ Aggregate: Mission      │  │ Aggregate: Session      │
│   └── MissionStage      │  │   └── SessionStage (VO) │
│       └── MissionClue   │  │   └── SessionParticipant│
└─────────────────────────┘  └─────────────────────────┘

┌─────────────────────────┐  ┌─────────────────────────┐
│   Teams BC              │  │   Trivia BC             │
│   Teams.Service         │  │   Trivia.Service        │
│                         │  │                         │
│ Entities: Team,         │  │ Aggregate: Quiz         │
│   TeamMember,           │  │   └── Question          │
│   Participant           │  │       └── Answer        │
│ External ref: Keycloak  │  │ Entities: Leaderboard,  │
│                         │  │   ParticipantAnswer     │
└─────────────────────────┘  └─────────────────────────┘
```

## Patrón de entidad de dominio

```csharp
public class Mission
{
    // 1. Constructor privado — solo factory methods crean instancias
    private Mission() { }

    // 2. Propiedades con setters privados
    public Guid Id { get; private set; }
    public string Title { get; private set; } = null!;
    public MissionStatus Status { get; private set; }

    // 3. Colecciones privadas, expuestas como IReadOnlyList
    private readonly List<MissionStage> _stages = new();
    public IReadOnlyList<MissionStage> Stages => _stages.AsReadOnly();

    // 4. Factory method (lógica de creación y validaciones)
    public static Mission Create(string title, string description, Difficulty difficulty, int timeMinutes, MissionType type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (timeMinutes <= 0) throw new ArgumentException("TimeMinutes must be positive");

        return new Mission
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Description = description,
            Difficulty = difficulty,
            TimeMinutes = timeMinutes,
            Type = type,
            Status = MissionStatus.Draft,
            CreatedAt = DateTime.UtcNow
        };
    }

    // 5. Métodos de comportamiento (lógica de negocio aquí, no en handlers)
    public void Activate()
    {
        if (Status != MissionStatus.Draft && Status != MissionStatus.Inactive)
            throw new InvalidOperationException($"Cannot activate mission in status {Status}");
        if (!_stages.Any())
            throw new InvalidOperationException("Mission must have at least one stage to be activated");

        Status = MissionStatus.Active;
    }

    public void AddStage(string name, string description, int order)
    {
        if (_stages.Any(s => s.Order == order))
            throw new InvalidOperationException($"Order {order} is already taken");

        _stages.Add(MissionStage.Create(Id, name, description, order));
    }
}
```

## Patrón de Value Object

```csharp
// Value Objects: identidad basada en valor, no en ID
// Son inmutables y se crean con factory methods

public sealed record JoinCode
{
    public string Value { get; }

    private JoinCode(string value) => Value = value;

    public static JoinCode Generate()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // sin O, 0, I, 1
        var random = new Random();
        var code = new string(Enumerable.Range(0, 6)
            .Select(_ => chars[random.Next(chars.Length)]).ToArray());
        return new JoinCode(code);
    }

    public static JoinCode From(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 6)
            throw new ArgumentException("JoinCode must be exactly 6 characters");
        return new JoinCode(value.ToUpperInvariant());
    }
}
```

## Referencias entre bounded contexts

Las referencias entre servicios son **IDs lógicos** (Guids), nunca objetos completos.

```csharp
// Sessions.Service referencia Missions.Service por ID
// NO importa el tipo Mission de Missions.Service
// En vez de eso, SessionStage COPIA los datos necesarios

public class SessionStage  // Value Object dentro de Session
{
    public Guid MissionId { get; private set; }    // referencia lógica
    public string MissionTitle { get; private set; } = null!; // copia desnormalizada
    public string MissionType { get; private set; } = null!;
    public int Order { get; private set; }
}
```

## State Pattern para Session

```csharp
// La máquina de estados está en la entidad Session
public class Session
{
    public SessionStatus Status { get; private set; }

    public void Prepare()
    {
        if (Status != SessionStatus.Scheduled)
            throw new InvalidOperationException($"Cannot prepare from {Status}");
        Status = SessionStatus.Preparing;
    }

    public void Start()
    {
        if (Status != SessionStatus.Preparing)
            throw new InvalidOperationException($"Cannot start from {Status}");
        Status = SessionStatus.Active;
        StartedAt ??= DateTime.UtcNow;  // solo se setea la primera vez
    }

    public void Pause()
    {
        if (Status != SessionStatus.Active)
            throw new InvalidOperationException($"Cannot pause from {Status}");
        Status = SessionStatus.Paused;
    }

    public void Resume()
    {
        if (Status != SessionStatus.Paused)
            throw new InvalidOperationException($"Cannot resume from {Status}");
        Status = SessionStatus.Active;
    }

    public void Finish()
    {
        if (Status != SessionStatus.Active && Status != SessionStatus.Paused)
            throw new InvalidOperationException($"Cannot finish from {Status}");
        Status = SessionStatus.Finished;
        EndedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == SessionStatus.Finished || Status == SessionStatus.Cancelled)
            throw new InvalidOperationException("Cannot cancel a terminal session");
        Status = SessionStatus.Cancelled;
    }
}
```

## Interfaz del repositorio (en Domain)

```csharp
// Domain define el contrato — nunca importa EF Core ni nada de Infrastructure
public interface IMissionRepository
{
    Task<Mission?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Mission>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(Mission mission, CancellationToken ct = default);
    Task UpdateAsync(Mission mission, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByTitleAsync(string title, CancellationToken ct = default);
}
```
