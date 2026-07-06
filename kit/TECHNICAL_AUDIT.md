# UMBRAL v2 — Auditoría Técnica Completa
> Documento generado para análisis de código. Contiene especificaciones exactas de arquitectura, tecnologías, endpoints, capas, WebSockets y estado de cobertura de tests.

---

## 1. ESTRUCTURA DEL PROYECTO

### 1.1 Repositorio
```
ProyectoUmbral/
├── src/
│   ├── ApiGateway/                  # YARP + JWT validation (:5000)
│   ├── ApiGateway.Tests/
│   ├── Missions.Service/            # CRUD misiones (:5001)
│   │   ├── Missions.Api/
│   │   ├── Missions.Api.Tests/
│   │   ├── Missions.Application/
│   │   ├── Missions.Application.Tests/
│   │   ├── Missions.Domain/
│   │   ├── Missions.Domain.Tests/
│   │   ├── Missions.Infrastructure/
│   │   ├── Missions.Infrastructure.Tests/
│   │   └── Missions.Service.slnx
│   ├── Sessions.Service/            # Ciclo de vida de sesiones (:5002)
│   │   ├── (misma estructura x8)
│   │   └── Sessions.Service.slnx
│   ├── Teams.Service/               # Equipos + Keycloak Admin API (:5003)
│   │   ├── (misma estructura x8)
│   │   └── Teams.Service.slnx
│   ├── Trivia.Service/              # Quizzes + Scoring + Leaderboard (:5004)
│   │   ├── (misma estructura x8)
│   │   └── Trivia.Service.slnx
│   └── RealTimeHub/                 # SignalR WebSocket hub (:5005)
│       └── RealTimeHub.Tests/
├── frontend/                        # React 19 + TypeScript (:5173)
├── keycloak/
│   └── realm-export.json            # Realm config auto-importado
├── kit/                             # Contexto para AI (no commitear graphify-out)
├── CLAUDE.md                        # Contexto para Claude Code
└── docker-compose.yml
```

### 1.2 Soluciones .slnx (una por microservicio)
- `src/Missions.Service/Missions.Service.slnx`
- `src/Sessions.Service/Sessions.Service.slnx`
- `src/Teams.Service/Teams.Service.slnx`
- `src/Trivia.Service/Trivia.Service.slnx`

> No hay solución raíz unificada. Tests se corren por solución: `dotnet test src/Sessions.Service/Sessions.Service.slnx`

---

## 2. ARQUITECTURA POR CAPAS (Hexagonal / Clean Architecture)

Cada microservicio sigue el mismo patrón de 4 capas:

```
┌──────────────────────────────────────────────────────┐
│  Api (Adaptador de entrada)                          │
│  → Program.cs, Endpoints, DI registration            │
├──────────────────────────────────────────────────────┤
│  Application (Orquestación de casos de uso)          │
│  → Commands, Queries, Handlers (MediatR), Validators │
│  → Interfaces de repositorios y servicios externos   │
├──────────────────────────────────────────────────────┤
│  Domain (Núcleo — sin dependencias externas)         │
│  → Entities, Value Objects, Enums, Domain Events     │
│  → Interfaces (contratos)                            │
├──────────────────────────────────────────────────────┤
│  Infrastructure (Implementaciones)                   │
│  → EF Core DbContext, Repositorios, RabbitMQ         │
│  → Keycloak HTTP client, HttpEventPublisher          │
└──────────────────────────────────────────────────────┘
```

**Regla de dependencias:** Domain ← Application ← Infrastructure ← Api
- Domain no conoce nada externo
- Application depende solo de Domain (via interfaces)
- Infrastructure implementa las interfaces de Domain/Application
- Api registra todo en DI y expone endpoints

---

## 3. STACK TECNOLÓGICO — DÓNDE SE USA CADA TECNOLOGÍA

### 3.1 ASP.NET Core 10 / .NET 10
- **Todos los microservicios**: Minimal APIs (no MVC Controllers)
- **Pattern**: `app.MapPost("/ruta", async (IMediator mediator, Request req) => ...)`
- **Archivos clave**: cada `*.Api/Program.cs` y `*.Api/Endpoints/*.cs`

### 3.2 MediatR + CQRS
- **Todos los microservicios** — separación estricta Command/Query
- **Commands** (escritura): `IRequest<TResponse>` → `IRequestHandler<TCommand, TResponse>`
- **Queries** (lectura): misma interfaz, nunca mezclar con Commands
- **Pipeline**: FluentValidation se ejecuta automáticamente antes del handler via `IPipelineBehavior`
- **Registro DI**: `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))`

### 3.3 FluentValidation
- **Todos los microservicios** — un validator por Command
- **Archivos**: `*.Application/*/CommandNameValidator.cs`
- **Registro**: `services.AddValidatorsFromAssembly(...)`
- **Integración**: `ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<...>` ejecuta antes del handler

### 3.4 Entity Framework Core 10 + PostgreSQL (Npgsql)
- **Missions.Infrastructure**: `MissionsDbContext` — tablas: Missions, MissionStages, MissionClues
- **Sessions.Infrastructure**: `SessionsDbContext` — tablas: Sessions, SessionParticipants; JSONB para Stages
- **Teams.Infrastructure**: `TeamsDbContext` — tablas: Participants, Teams, TeamMembers
- **Trivia.Infrastructure**: `TriviaDbContext` — tablas: Quizzes, Questions, Answers, ParticipantAnswers, LeaderboardEntries
- **Configuración**: `IEntityTypeConfiguration<T>` por entidad, `EnsureCreated()` (no migraciones)
- **JSONB**: `SessionStage` usa `OwnsMany(...).ToJson("Stages")` — es Value Object, no tabla separada

### 3.5 SignalR (WebSockets) — Ver Sección 6

### 3.6 RabbitMQ — Ver Sección 7

### 3.7 YARP (API Gateway)
- **Archivo**: `src/ApiGateway/Program.cs` + `appsettings.json` (sección `ReverseProxy`)
- **Rol**: único punto de entrada externo, valida JWT antes de rutear
- **Rutas**: `/missions/**` → missions:5001, `/sessions/**` → sessions:5002, etc.
- **No tiene base de datos ni lógica de negocio**

### 3.8 Keycloak (OIDC + Authorization Code)
- **ApiGateway**: valida JWT via `AddJwtBearer`, transforma claims con `KeycloakRolesTransformer`
- **Teams.Service**: llama Admin REST API para crear/deshabilitar usuarios
- **Frontend**: `oidc-client-ts` + `UserManager` para Authorization Code Flow
- **Realm**: `umbral` | Clientes: `umbral-frontend` (public), `umbral-gateway` (confidential)
- **Roles**: `admin`, `operator`, `participant` (Realm Roles)

### 3.9 NSubstitute + xUnit + FluentAssertions (Tests)
- **Todos los servicios**: mocking con NSubstitute (NO Moq)
- **Pattern**: `Substitute.For<IInterface>()`, `.Received(1).Method(Arg.Any<T>())`
- **Naming**: `Given_Context_When_Action_Then_Result`
- **DB tests**: EF Core InMemory database o repositorios mockeados

### 3.10 React 19 + TypeScript 6 + Vite 8 (Frontend)
- **Autenticación**: `oidc-client-ts` + `react-oidc-context`
- **Routing**: `react-router-dom` v7
- **HTTP**: `fetchWithAuth()` interceptor con auto-refresh de token
- **Real-time**: `@microsoft/signalr` via `useSignalR.ts`
- **State management**: `useReducer` via `GameContext.tsx` (sin Redux)
- **Tests**: Vitest + @testing-library/react

---

## 4. MICROSERVICIO: MISSIONS.SERVICE (:5001)

### 4.1 Dominio
**Entidades** (`src/Missions.Service/Missions.Domain/Entities/`):

```csharp
// Mission.cs — Aggregate Root, implementa IMissionComponent (Composite Pattern)
public class Mission
{
    public Guid Id { get; private set; }
    public string Title { get; private set; }           // MaxLength(200), único
    public string Description { get; private set; }    // MaxLength(2000)
    public Difficulty Difficulty { get; private set; } // Easy | Medium | Hard
    public int TimeMinutes { get; private set; }       // solo [15, 30, 60, 90]
    public MissionType Type { get; private set; }      // Treasure | Trivia
    public MissionStatus Status { get; private set; }  // Draft | Active | Inactive
    public DateTime CreatedAt { get; private set; }
    public IReadOnlyList<MissionStage> Stages { get; } // Colección privada
    // Métodos: Create(), Update(), SetStatus(), AddStage(), UpdateStage(), AddStageClue()
}

// MissionStage.cs — implementa IMissionComponent
public class MissionStage
{
    public Guid Id { get; private set; }
    public Guid MissionId { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public int Order { get; private set; }
    public IReadOnlyList<MissionClue> Clues { get; }
    // Métodos: AddClue(), RemoveClue(), GetTotalPenalty(), GetLeafCount()
}

// MissionClue.cs — implementa IMissionComponent (hoja)
public class MissionClue
{
    public Guid Id { get; private set; }
    public Guid StageId { get; private set; }
    public string Content { get; private set; }
    public int? Penalty { get; private set; }
    public ReleaseType ReleaseType { get; private set; }  // Auto | Manual
}
```

**Enums**: `Difficulty {Easy,Medium,Hard}`, `MissionType {Treasure,Trivia}`, `MissionStatus {Draft,Active,Inactive}`, `ReleaseType {Auto,Manual}`

**Patrón**: Composite Pattern — `IMissionComponent` permite tratar Mission, Stage y Clue uniformemente para `GetTotalPenalty()` y `GetLeafCount()`

### 4.2 Repositorio
```csharp
// src/Missions.Service/Missions.Application/Common/Interfaces/IMissionRepository.cs
public interface IMissionRepository
{
    Task AddAsync(Mission mission, CancellationToken ct);
    Task<bool> IsTitleUniqueAsync(string title, CancellationToken ct, Guid? excludeId = null);
    Task<(IReadOnlyList<Mission> Missions, int TotalCount)> GetMissionsAsync(
        string? search, string? difficulty, string? status, int page, int pageSize, CancellationToken ct);
    Task<Mission?> GetByIdAsync(Guid id, CancellationToken ct);
    Task UpdateAsync(Mission mission, CancellationToken ct);
    Task AddStageAsync(Mission mission, CancellationToken ct);
    Task AddClueAsync(Mission mission, Guid stageId, CancellationToken ct);
    void RemoveStage(Mission mission, MissionStage stage);
    Task SaveChangesAsync(CancellationToken ct);
}
```

### 4.3 Commands / Queries / Handlers
| Nombre | Tipo | Request | Response |
|--------|------|---------|----------|
| `CreateMissionCommand` | Command | Title, Description, Difficulty, TimeMinutes, Type | CreateMissionCommandResult |
| `UpdateMissionCommand` | Command | Id, Title, Description, Difficulty, TimeMinutes | void |
| `ChangeMissionStatusCommand` | Command | Id, NewStatus | void |
| `CreateStageCommand` | Command | MissionId, Name, Description, Order | CreateStageCommandResult |
| `UpdateStageCommand` | Command | MissionId, StageId, Name, Description, Order | UpdateStageCommandResult |
| `DeleteStageCommand` | Command | MissionId, StageId | void |
| `CreateClueCommand` | Command | MissionId, StageId, Content, Penalty?, ReleaseType | CreateClueCommandResult |
| `DeleteClueCommand` | Command | MissionId, StageId, ClueId | void |
| `GetMissionsQuery` | Query | search?, difficulty?, status?, page, pageSize | GetMissionsResult (paginado) |
| `GetMissionByIdQuery` | Query | Id | MissionDetailDto |

### 4.4 Endpoints HTTP
Todos en `src/Missions.Service/Missions.Api/Endpoints/` — Política: `RequireAuthorization("admin")`

| Método | Ruta | Dispatch | HTTP |
|--------|------|----------|------|
| POST | `/missions` | CreateMissionCommand | 201 |
| GET | `/missions` | GetMissionsQuery | 200 (paginado) |
| GET | `/missions/{id}` | GetMissionByIdQuery | 200 |
| PUT | `/missions/{id}` | UpdateMissionCommand | 200 |
| PATCH | `/missions/{id}/status` | ChangeMissionStatusCommand | 200 |
| POST | `/missions/{id}/stages` | CreateStageCommand | 201 |
| PUT | `/missions/{id}/stages/{stageId}` | UpdateStageCommand | 200 |
| DELETE | `/missions/{id}/stages/{stageId}` | DeleteStageCommand | 204 |
| POST | `/missions/{id}/stages/{stageId}/clues` | CreateClueCommand | 201 |
| DELETE | `/missions/{id}/stages/{stageId}/clues/{clueId}` | DeleteClueCommand | 204 |

### 4.5 Validadores FluentValidation
- `CreateMissionCommandValidator`: Title (NotEmpty, MaxLength 200), Description (MaxLength 2000), Difficulty (enum válido), TimeMinutes (solo [15,30,60,90]), Type (enum válido)
- Validators adicionales para Update, Stage, Clue, StatusChange

### 4.6 DbContext y EF Core
```csharp
// MissionsDbContext.cs
public DbSet<Mission> Missions => Set<Mission>();
public DbSet<MissionStage> MissionStages => Set<MissionStage>();
public DbSet<MissionClue> MissionClues => Set<MissionClue>();

// SessionConfiguration: Title MaxLength(200), enums como string, Index en Title
```

---

## 5. MICROSERVICIO: SESSIONS.SERVICE (:5002)

### 5.1 Dominio
```csharp
// Session.cs — Aggregate Root
public class Session
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Pin { get; private set; }               // 6 dígitos, único
    public IReadOnlyList<SessionStage> Stages { get; }    // JSONB — Value Objects
    public int CurrentStageOrder { get; private set; }
    public SessionStatus Status { get; set; }
    private ISessionState _state;                          // State Pattern [NotMapped]
    public DateTime? StartedAt { get; internal set; }
    public DateTime? EndedAt { get; internal set; }
    public DateTime CreatedAt { get; private set; }
    public IReadOnlyList<SessionParticipant> Participants { get; }
    // Métodos: Create(), TransitionTo(), AddParticipant(), GetCurrentStage(), AdvanceStage()
}

// SessionStage.cs — Value Object (no tabla, JSONB)
public class SessionStage
{
    public Guid MissionId { get; private set; }
    public string MissionTitle { get; private set; }
    public string MissionType { get; private set; }
    public int Order { get; private set; }
}

// SessionParticipant.cs — Entidad separada (tabla propia)
public class SessionParticipant
{
    public Guid Id { get; private set; }
    public Guid SessionId { get; private set; }
    public Guid UserId { get; private set; }          // ref Keycloak, no FK real
    public DateTime JoinedAt { get; private set; }
}
```

**Enums**: `SessionStatus {Scheduled, Preparing, Active, Paused, Finished, Cancelled}`

### 5.2 State Pattern — Máquina de Estados
**Interfaz**: `ISessionState` con `Status`, `CanTransitionTo(target)`, `OnEnter(session)`, `OnExit(session)`

**Implementaciones** (`src/Sessions.Service/Sessions.Domain/States/`):
```
ScheduledState  → puede ir a: Preparing, Cancelled
PreparingState  → puede ir a: Active, Cancelled
ActiveState     → puede ir a: Paused, Finished
PausedState     → puede ir a: Active, Cancelled
FinishedState   → Terminal (no transiciones)
CancelledState  → Terminal (no transiciones)
```

**StateFactory.cs**: `Dictionary<SessionStatus, ISessionState>` estático — singletons por estado

**Activación**: `session.TransitionTo(newStatus)` → llama `_state.CanTransitionTo()` → `_state.OnExit()` → setea nuevo estado → `newState.OnEnter()`

### 5.3 Chain of Responsibility (Validaciones de transición)
```
TransitionSessionCommand
  → ValidStatusHandler (¿es un SessionStatus válido?)
    → NotTerminalHandler (¿está en estado terminal?)
      → [si pasa] → session.TransitionTo()
```
- **Interfaz**: `IStateTransitionHandler` con `Handle(session, newStatus)` y `SetNext(handler)`
- **Archivos**: `src/Sessions.Service/Sessions.Application/Sessions/Transition/Handlers/`

### 5.4 JSONB para SessionStage
```csharp
// SessionConfiguration.cs
builder.OwnsMany(s => s.Stages, stage =>
{
    stage.ToJson("Stages");  // EF Core 8+ — serializa como columna JSONB
});
```
- **Ventaja**: evita tabla separada, SessionStage no tiene ciclo de vida propio
- **Desventaja**: no indexable, queries dentro de Stages requieren full scan

### 5.5 Commands / Queries / Handlers
| Nombre | Tipo | Request | Response |
|--------|------|---------|----------|
| `CreateSessionCommand` | Command | Name, `List<StageInput>` (MissionId, MissionTitle, MissionType, Order) | CreateSessionCommandResult (Id, Name, Pin, Stages) |
| `TransitionSessionCommand` | Command | Id, NewStatus | void |
| `JoinSessionCommand` | Command | SessionId, UserId | JoinSessionCommandResult |
| `StartSessionCommand` | Command | SessionId | void |
| `FinishSessionCommand` | Command | SessionId | void |
| `AdvanceStageCommand` | Command | SessionId | void |
| `ReleaseClueCommand` | Command | SessionId, StageId, TeamId | void |
| `GetSessionsQuery` | Query | — | lista SessionListItemDto |
| `GetSessionProgressQuery` | Query | SessionId | SessionProgressDto |

### 5.6 Notificaciones al RealTimeHub (HTTP)
```csharp
// HttpEventPublisher.cs — src/Sessions.Service/Sessions.Infrastructure/Messaging/
public async Task PublishAsync(string eventName, object payload, CancellationToken ct)
{
    var path = eventName switch
    {
        "SessionStarted"    => "/internal/notifications/session-status",
        "ProgressUpdated"   => "/internal/notifications/progress",
        "ClueReleased"      => "/internal/notifications/clue-released",
        _                   => $"/internal/events/{eventName}"
    };
    await _client.PostAsync(path, content, ct);  // Fire-and-forget, sin retry
}

// GameNotifier.cs — fachada sobre HttpEventPublisher
public async Task NotifySessionStatusChanged(Guid sessionId, string status, ...)
public async Task NotifyProgressUpdated(Guid sessionId, object progressData, ...)
public async Task NotifyClueReleased(Guid sessionId, Guid? teamId, object clueData, ...)
public async Task NotifyQuestionClosed(Guid sessionId, Guid questionId, Guid correctAnswerId, ...)
```

### 5.7 Endpoints HTTP
Política: `RequireAuthorization("admin")` salvo `/join` que es `participant`

| Método | Ruta | Dispatch | HTTP |
|--------|------|----------|------|
| POST | `/sessions` | CreateSessionCommand | 201 |
| GET | `/sessions` | GetSessionsQuery | 200 |
| GET | `/sessions/{id}` | GetSessionByIdQuery | 200 |
| GET | `/sessions/{id}/progress` | GetSessionProgressQuery | 200 |
| POST | `/sessions/{id}/transition` | TransitionSessionCommand | 200 |
| POST | `/sessions/{id}/join` | JoinSessionCommand | 200 |
| POST | `/sessions/{id}/start` | StartSessionCommand | 200 |
| POST | `/sessions/{id}/finish` | FinishSessionCommand | 200 |
| POST | `/sessions/{id}/advance-stage` | AdvanceStageCommand | 200 |
| POST | `/sessions/{id}/clues/release` | ReleaseClueCommand | 200 |

### 5.8 Publicación de eventos a RabbitMQ
Sessions.Service publica `session.status.changed` al exchange `trivia.exchange` (topic) cuando cambia estado

---

## 6. WEBSOCKETS / SIGNALR — REALTIMEHUB (:5005)

### 6.1 Configuración (Program.cs)
```csharp
// src/RealTimeHub/Program.cs
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(p => p
        .AllowAnyHeader().AllowAnyMethod()
        .AllowCredentials().SetIsOriginAllowed(_ => true)));

app.UseCors();
app.MapHub<GameHub>("/hub/game");       // WebSocket endpoint
app.MapNotificationEndpoints();         // HTTP endpoints internos
```

### 6.2 GameHub — src/RealTimeHub/Hubs/GameHub.cs
```csharp
public class GameHub : Hub
{
    // Cliente → Servidor
    public async Task JoinSessionGroup(string sessionId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);

    public async Task LeaveSessionGroup(string sessionId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
}
```

### 6.3 Eventos Servidor → Cliente (con payloads)
Todos se emiten via `IHubContext<GameHub>` en los endpoints internos:

| Evento SignalR | Trigger | Payload |
|----------------|---------|---------|
| `SessionStatusChanged` | Sessions.Service cambia estado | `{ sessionId: Guid, status: string }` |
| `ProgressUpdated` | Sessions.Service avanza etapa | `{ sessionId: Guid, progressData: object }` |
| `ClueReleased` | Sessions.Service libera pista | `{ sessionId: Guid, teamId?: Guid, clueData: object }` |
| `QuestionAsked` | Trivia.Service lanza pregunta | `{ questionId, text, options, timeLimitSeconds, sessionId, askedAt }` |
| `QuestionClosed` | Trivia.Service cierra pregunta | `{ questionId: Guid, correctAnswerId: Guid, correctAnswerText?: string }` |
| `RankingUpdated` | TriviaAnswerSubmittedConsumer | `List<LeaderboardEntryDto>` |

### 6.4 Endpoints HTTP Internos (llamados por otros microservicios)
Archivo: `src/RealTimeHub/Endpoints/NotificationEndpoints.cs`

| Método | Ruta | Llamado por |
|--------|------|------------|
| POST | `/internal/notifications/session-status` | Sessions.Service |
| POST | `/internal/notifications/progress` | Sessions.Service |
| POST | `/internal/notifications/clue-released` | Sessions.Service |
| POST | `/internal/notifications/question-asked` | Trivia.Service |
| POST | `/internal/notifications/question-closed` | Trivia.Service |
| POST | `/internal/events/LeaderboardUpdated` | Trivia.Service (via Consumer) |

### 6.5 Records de Payload
```csharp
record SessionStatusNotification(Guid SessionId, string Status);
record ProgressNotification(Guid SessionId, object ProgressData);
record ClueReleasedNotification(Guid SessionId, Guid? TeamId, object ClueData);
record QuestionAskedNotification(Guid SessionId, Guid QuestionId, string QuestionText,
    string[] Options, int TimeLimitSeconds, DateTime AskedAt);
record QuestionClosedNotification(Guid SessionId, Guid QuestionId,
    Guid CorrectAnswerId, string? CorrectAnswerText);
record LeaderboardEntryDto(Guid Id, Guid QuizId, Guid TeamId, string? TeamName,
    int Score, DateTime UpdatedAt);
```

### 6.6 Frontend — useSignalR.ts
**Path**: `frontend/src/hooks/useSignalR.ts`

```typescript
const hub = new HubConnectionBuilder()
    .withUrl("/hub/game", {
        accessTokenFactory: async () => (await userManager.getUser())?.access_token ?? ""
    })
    .withAutomaticReconnect([0, 1000, 2000, 4000, 8000, 15000, 30000])  // 7 reintentos
    .build();

// Escucha: SessionStatusChanged, ProgressUpdated, ClueReleased,
//          QuestionClosed, QuestionAsked, RankingUpdated
// Envía: hub.invoke("JoinSessionGroup", sessionId)
//        hub.invoke("LeaveSessionGroup", sessionId)
```

---

## 7. RABBITMQ — MENSAJERÍA ASÍNCRONA

### 7.1 Topología
```
Exchange: "trivia" (type: topic)
  └── Binding: routing key "answer.submitted" → Queue: "trivia.answer.submitted"

Sessions.Service también publica:
  Exchange: "trivia.exchange" (topic)
    └── routing key: "session.status.changed"
```

### 7.2 Publisher — Trivia.Service
**Path**: `src/Trivia.Service/Trivia.Infrastructure/Messaging/RabbitMQ/RabbitMqEventPublisher.cs`

```csharp
public class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    // Exchange: "trivia", routing key: "answer.submitted"
    public Task PublishAsync(string eventName, object payload, CancellationToken ct)
    {
        var routingKey = eventName == "TriviaAnswerSubmittedEvent" ? "answer.submitted" : eventName;
        var body = JsonSerializer.SerializeToUtf8Bytes(payload, camelCase);
        _channel.BasicPublish("trivia", routingKey, props, body);
        return Task.CompletedTask;
    }
}
```

### 7.3 Consumers — Trivia.Service (⚠️ duplicados)

**TypedTriviaAnswerSubmittedConsumer** (recomendado — type-safe):
```csharp
// BackgroundService, EventingBasicConsumer (event-driven, no polling)
// BasicQos(0, 1, false) — procesa 1 mensaje por vez
// Registrado: services.AddHostedService<TypedTriviaAnswerSubmittedConsumer>()
// Condición: solo si RABBITMQ_HOST env var está configurada
```

**TriviaAnswerSubmittedConsumer** (legacy — reflection-based):
```csharp
// BackgroundService, BasicGet con polling cada 500ms
// Usa reflection para compatibilidad de versiones
// ⚠️ Duplica lógica con el TypedConsumer
```

### 7.4 Flujo Completo End-to-End
```
1. Participante POST /answers (SubmitAnswerCommand)
   ↓
2. SubmitAnswerCommandHandler:
   - Valida respuesta (IsCorrect)
   - Guarda ParticipantAnswer en DB
   - Publica TriviaAnswerSubmittedEvent → RabbitMQ (exchange:"trivia", key:"answer.submitted")
   - Calcula score con TimeBasedScoringStrategy
   - Actualiza LeaderboardEntry directamente
   - POST /internal/events/LeaderboardUpdated → RealTimeHub (HTTP síncrono)
   ↓
3. [Asíncrono] TypedTriviaAnswerSubmittedConsumer consume mensaje:
   - UpdateLeaderboardCommand
   - Vuelve a notificar RealTimeHub
   ↓
4. RealTimeHub → SignalR → todos los clientes en el grupo
   - Evento: "RankingUpdated"
   - Payload: List<LeaderboardEntryDto>
```

---

## 8. MICROSERVICIO: TEAMS.SERVICE (:5003)

### 8.1 Entidades
```csharp
// Team.cs
public class Team
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }          // Único
    public string Description { get; private set; }
    public string LeaderId { get; private set; }      // Keycloak User ID (string)
    public string? JoinCode { get; private set; }     // 6 chars alfanumérico, auto-generado
    public DateTime CreatedAt { get; private set; }
    public IReadOnlyList<TeamMember> Members { get; }
    // Métodos: Create(), AddMember(), RemoveMember(), Update(), GenerateJoinCode()
}

// TeamMember.cs
public class TeamMember
{
    public Guid TeamId { get; private set; }
    public Guid Id { get; private set; }
    public string UserId { get; private set; }    // Keycloak ID
    public DateTime JoinedAt { get; private set; }
}

// Participant.cs — copia local del usuario Keycloak
public class Participant
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Alias { get; private set; }         // Único entre participantes
    public string Email { get; private set; }         // Único
    public string KeycloakUserId { get; private set; }
    public DateTime RegisteredAt { get; private set; }
    public bool IsActive { get; private set; }
}
```

### 8.2 Integración Keycloak Admin API
```csharp
// IKeycloakAdminService.cs (interfaz en Application)
Task<string> CreateUserAsync(string username, string email, string password, string name, CancellationToken ct);
Task DeleteUserAsync(string userId, CancellationToken ct);
Task AssignRoleAsync(string userId, string roleName, CancellationToken ct);

// RegisterParticipantCommandHandler — Saga Manual (Compensating Transaction):
// 1. Verifica alias/email únicos localmente
// 2. Crea usuario en Keycloak (POST /admin/realms/umbral/users)
// 3. Asigna role "participant"
// 4. Guarda Participant en DB local
// 5. Si paso 4 falla → DELETE Keycloak user (compensación)
```

### 8.3 Endpoints
| Método | Ruta | Política | Dispatch |
|--------|------|----------|----------|
| POST | `/auth/register` | Anonymous | RegisterParticipantCommand |
| GET | `/profile` | participant | GetProfileQuery |
| PUT | `/profile` | participant | UpdateProfileCommand |
| POST | `/teams` | authenticated | CreateTeamCommand |
| PUT | `/teams/{id}` | admin | UpdateTeamCommand |
| GET | `/teams` | authenticated | GetTeamsQuery |
| GET | `/teams/{id}` | authenticated | GetTeamByIdQuery |
| POST | `/teams/{id}/join` | participant | JoinTeamCommand |
| POST | `/operators` | admin | CreateOperatorCommand |
| DELETE | `/operators/{email}` | admin | DisableOperatorCommand |
| GET | `/users` | admin | GetUsersQuery |
| GET | `/users/{id}` | admin | GetUserByIdQuery |

---

## 9. MICROSERVICIO: TRIVIA.SERVICE (:5004)

### 9.1 Entidades
```csharp
public class Quiz     { Guid Id; string? Title; }
public class Question { Guid Id; Guid QuizId; string? Text; int TimeLimitSeconds; DateTime? ReleasedAt; }
public class Answer   { Guid Id; Guid QuestionId; string? Text; int DisplayOrder; }
public class ParticipantAnswer { Guid Id; Guid QuizId; Guid TeamId; Guid QuestionId;
    Guid AnswerId; DateTime Timestamp; bool IsCorrect; }
public class LeaderboardEntry { Guid QuizId; Guid TeamId; string? TeamName;
    int Score; DateTime UpdatedAt; }
```

### 9.2 Scoring Strategy
```csharp
public interface IScoringStrategy
{
    int CalculateScore(TimeSpan timeElapsed, int timeLimitSeconds);
}

public class TimeBasedScoringStrategy : IScoringStrategy
{
    public int CalculateScore(TimeSpan timeElapsed, int timeLimitSeconds)
    {
        var percentageUsed = timeElapsed.TotalSeconds / timeLimitSeconds;
        if (percentageUsed > 1.0) return 0;           // Timeout
        return (int)Math.Round(100 * (1 - percentageUsed));  // Más rápido = más puntos
    }
}
```

### 9.3 Caché estático de respuestas correctas (⚠️ problema de escalabilidad)
```csharp
// AskQuestionCommandHandler.cs
public static ConcurrentDictionary<Guid, int> CorrectAnswers = new();
public static ConcurrentDictionary<Guid, DateTime> CorrectAnswerTimestamps = new();
// En memoria, no persiste reinicios, no escala a múltiples instancias
```

### 9.4 Endpoints
| Método | Ruta | Política | Dispatch |
|--------|------|----------|----------|
| GET | `/quizzes` | authenticated | GetQuizzesQuery |
| POST | `/quizzes/{id}/start` | admin | StartTriviaCommand |
| POST | `/quizzes/{id}/questions/{qId}/ask` | admin | AskQuestionCommand |
| POST | `/quizzes/{id}/questions/{qId}/close` | admin | CloseQuestionCommand |
| POST | `/quizzes/{id}/answers/submit` | participant | SubmitAnswerCommand |
| GET | `/quizzes/{id}/leaderboard` | authenticated | GetLeaderboardQuery |
| POST | `/quizzes/{id}/end` | admin | EndTriviaGameCommand |

---

## 10. API GATEWAY (:5000)

### 10.1 KeycloakRolesTransformer.cs
```csharp
// src/ApiGateway/KeycloakRolesTransformer.cs
public sealed class KeycloakRolesTransformer : IClaimsTransformation
{
    // JWT contiene: realm_access: {"roles": ["admin", "operator"]}
    // Transforma a ClaimTypes.Role estándar de .NET
    // Registrado como: services.AddScoped<IClaimsTransformation, KeycloakRolesTransformer>()
}
```

### 10.2 Políticas de Autorización
```csharp
options.AddPolicy("authenticated",           p => p.RequireAuthenticatedUser());
options.AddPolicy("admin",                   p => p.RequireRole("admin"));
options.AddPolicy("operator",                p => p.RequireRole("operator"));
options.AddPolicy("participant",             p => p.RequireRole("participant"));
options.AddPolicy("operator_or_participant", p => p.RequireAssertion(ctx =>
    ctx.User.IsInRole("operator") || ctx.User.IsInRole("participant")));
options.AddPolicy("operator_or_admin",       p => p.RequireAssertion(ctx =>
    ctx.User.IsInRole("admin") || ctx.User.IsInRole("operator")));
```

### 10.3 YARP Routing
| Ruta Entrante | Cluster Destino | Puerto |
|---------------|-----------------|--------|
| `/missions/**` | missions-cluster | 5001 |
| `/sessions/**` | sessions-cluster | 5002 |
| `/teams/**` | teams-cluster | 5003 |
| `/trivia/**` | trivia-cluster | 5004 |
| `/hub/**` (WebSocket) | hub-cluster | 5005 |

---

## 11. FRONTEND

### 11.1 Estructura de Archivos (60 archivos .ts/.tsx)
```
frontend/src/
├── auth/
│   ├── AuthProvider.tsx        — OidcProvider wrapper
│   ├── keycloak.ts             — UserManager config (PKCE desactivado en dev)
│   ├── ProtectedRoute.tsx      — Route guard por rol
│   └── useAuth.ts              — Hook: user, token, isAuthenticated, roles, logout
├── hooks/
│   └── useSignalR.ts           — HubConnection lifecycle, 7 eventos, reconexión automática
├── contexts/
│   └── GameContext.tsx         — useReducer con 11 action types (estado global del juego)
├── services/
│   ├── api.ts                  — fetchWithAuth con auto-refresh de token
│   ├── missionsApi.ts / sessionsApi.ts / teamsApi.ts / ...
├── pages/
│   ├── admin/ (16 vistas)
│   ├── operator/ (2 vistas)
│   ├── participant/ (4 + 4 vistas en game/)
│   └── public/ (1 vista)
├── components/game/
│   ├── ClueCard.tsx / CountdownTimer.tsx / QuestionCard.tsx
│   ├── RankingBoard.tsx / Timer.tsx
└── types/ (7 archivos de tipos TypeScript)
```

### 11.2 Flujo de Autenticación
```
1. Usuario no autenticado → ProtectedRoute redirige a Keycloak
2. Keycloak Authorization Code Flow → /callback
3. onSigninCallback → guarda user en sessionStorage
4. fetchWithAuth extrae token, lo agrega como Bearer
5. Si 401 → signinSilent() → reintenta request
6. Si refresh falla → signinRedirect()
```

### 11.3 GameContext Reducer (Estado del juego)
```typescript
type GameAction =
  | { type: "STATUS_CHANGED"; status: SessionStatus }
  | { type: "PROGRESS_UPDATED"; data: unknown }
  | { type: "CLUE_RELEASED"; clue: unknown }
  | { type: "CONNECTION_STATE_CHANGED"; state: ConnectionState }
  | { type: "TICK" }
  | { type: "RANKING_UPDATED"; ranking: RankingEntry[] }
  | { type: "QUESTION_RECEIVED"; question: TriviaQuestion }
  | { type: "ANSWER_SELECTED"; answerIndex: number }
  | { type: "QUESTION_CLEARED" }
  // + SESSION_LOADED, SET_SCORE
```

### 11.4 GameView — Flujo de Estados de Vista
```typescript
switch (state.sessionStatus) {
    case "Scheduled":
    case "Preparing":  return <WaitingRoom />;
    case "Active":
    case "Paused":     return <ActiveGame />;
    case "Finished":
    case "Cancelled":  return <GameResults />;
}
```

---

## 12. COBERTURA DE TESTS

### 12.1 Estadísticas Generales
- **Total archivos de test**: 133
- **Framework**: xUnit + NSubstitute + FluentAssertions
- **Naming**: `Given_Context_When_Action_Then_Result`
- **Proyectos**: `*.Domain.Tests`, `*.Application.Tests`, `*.Infrastructure.Tests`, `*.Api.Tests`

### 12.2 Tests por Servicio

| Servicio | Domain | Application | Infraestructure | API | Est. Cobertura |
|----------|--------|-------------|-----------------|-----|----------------|
| Missions | ✅ ~90% | ✅ ~85% | ✅ ~75% | ✅ ~70% | **80%** |
| Sessions | ✅ ~85% | ✅ ~80% | ⚠️ ~60% | ⚠️ ~65% | **73%** |
| Teams | ⚠️ ~75% | ⚠️ ~70% | ❌ ~50% | ⚠️ ~60% | **64%** |
| Trivia | ✅ ~80% | ✅ ~75% | ✅ ~65% | ❌ ~55% | **69%** |
| ApiGateway | — | — | — | ⚠️ ~30% | **30%** |
| RealTimeHub | — | — | — | ⚠️ ~40% | **40%** |
| Frontend | — | — | — | ❌ ~5% | **5%** |

**Promedio backend**: ~66% | **Meta**: 90%

### 12.3 Qué falta para llegar al 90%
1. **Tests de API** para todos los endpoints (WebApplicationFactory)
2. **Tests de integración** Sessions + RealTimeHub (HTTP mock)
3. **Tests de Infrastructure** para Teams (Keycloak mock)
4. **Tests de ApiGateway** más exhaustivos (todas las políticas)
5. **Tests Frontend** — React components con @testing-library (actual: 4 tests)

### 12.4 Tests Clave Existentes
- `StateFactoryTests` — todas las transiciones de estado de Session
- `TransitionSessionCommandHandlerPublishTests` — verifica publicación de eventos
- `SubmitAnswerCommandHandlerTests` + `ScoringTests` + `EdgeTests`
- `TimeBasedScoringStrategyTests`
- `RabbitMqEventPublisherTests`
- `KeycloakRolesTransformerTests`
- `GameHubTests` + `NotificationEndpointsTests`

---

## 13. PROBLEMAS ESTRUCTURALES IDENTIFICADOS

### 🔴 Críticos

**P1 — Consumers RabbitMQ duplicados (Trivia.Service)**
- `TriviaAnswerSubmittedConsumer` (polling BasicGet, reflection) Y `TypedTriviaAnswerSubmittedConsumer` (EventingBasicConsumer) procesan el mismo evento
- Ambos pueden estar activos → doble procesamiento de leaderboard
- **Fix**: eliminar `TriviaAnswerSubmittedConsumer`, mantener solo `TypedTriviaAnswerSubmittedConsumer`

**P2 — Scoring cache en memoria estático (Trivia.Service)**
```csharp
// AskQuestionCommandHandler.cs
public static ConcurrentDictionary<Guid, int> CorrectAnswers = new();
```
- No persiste entre reinicios del servicio
- No escala a múltiples instancias del servicio
- **Fix**: persistir en DB o usar Redis

**P3 — HttpEventPublisher sin retry (Sessions.Service)**
```csharp
catch { /* Intentionally ignore */ }
```
- Si RealTimeHub está caído, los eventos se pierden silenciosamente
- **Fix**: Polly retry con exponential backoff, o Outbox Pattern

### 🟡 Importantes

**P4 — Sin transacciones distribuidas (Sessions y Trivia)**
- `UpdateAsync(session)` + `PublishAsync(event)` no son atómicos
- Si DB commit exitoso pero event publish falla → estado inconsistente
- **Fix**: Outbox Pattern (guardar evento en misma transacción DB, publisher en background)

**P5 — Endpoints internos de RealTimeHub sin autenticación**
- `POST /internal/notifications/*` accesibles desde exterior
- **Fix**: middleware que valide IP de origen o token interno

**P6 — State Pattern singletons compartidos**
```csharp
// StateFactory.cs — instancias compartidas entre todas las Sessions
[SessionStatus.Scheduled] = new ScheduledState(),  // singleton
```
- Aceptable si los estados son stateless (verificar que OnEnter/OnExit no mutan estado)

### 🟢 Menores

**P7 — Quiz entity incompleta**
```csharp
public class Quiz { public Guid Id; public string? Title; }
// Falta: Questions navigation, CreatedAt, metadata
```

**P8 — Validadores inconsistentes entre servicios**
- TimeMinutes solo permite [15,30,60,90] en Missions pero no hay validación uniforme de tipos

**P9 — Sin retry automático en frontend para fallos de SignalR**
- `withAutomaticReconnect` configurado ✅ pero sin circuit breaker para errores persistentes

---

## 14. ANÁLISIS DE ESCALABILIDAD

### 14.1 Puntos Fuertes
- ✅ Database per Service — cada BD escala independientemente
- ✅ RabbitMQ desacopla Trivia.Service del procesamiento síncrono
- ✅ YARP Gateway — puede balancear carga con múltiples instancias
- ✅ SignalR grupos por sesión — no broadcast global
- ✅ JWT stateless — el Gateway no necesita estado

### 14.2 Cuellos de Botella
- ❌ `AskQuestionCommandHandler.CorrectAnswers` — ConcurrentDictionary estático, no funciona con múltiples instancias de Trivia.Service
- ❌ `EnsureCreated()` — bloquea en startup, no soporta evolución de schema
- ❌ RealTimeHub sin persistencia — si se reinicia, pierde todas las conexiones activas
- ❌ Keycloak single point of failure — si cae, nadie puede autenticarse
- ❌ JSONB para SessionStage — queries `WHERE stage.missionId = X` requieren full table scan

### 14.3 Para alcanzar 90% de cobertura de tests
```
Prioridad 1 (impacto alto):
  - API Tests con WebApplicationFactory para cada servicio (~20 tests)
  - Integration tests Sessions → RealTimeHub (mock HTTP)
  - Tests para Teams Infrastructure (Keycloak mock exhaustivo)

Prioridad 2 (completar cobertura):
  - Frontend component tests (react-testing-library)
  - Consumer tests (TypedTriviaAnswerSubmittedConsumer)
  - GameNotifier con mock de HttpEventPublisher
  - Endpoint tests del ApiGateway (todas las políticas)
```

---

## 15. INFRAESTRUCTURA DOCKER

### 15.1 Servicios en docker-compose.yml
| Servicio | Puerto(s) | Imagen |
|----------|-----------|--------|
| API Gateway | 5000 | .NET build |
| Missions Service | 5001 | .NET build |
| Sessions Service | 5002 | .NET build |
| Teams Service | 5003 | .NET build |
| Trivia Service | 5004 | .NET build |
| RealTimeHub | 5005 | .NET build |
| Frontend | 5173 | Node/Vite |
| Keycloak | 8080 | quay.io/keycloak/keycloak:26.1 |
| keycloak-db | — | postgres:16-alpine |
| missions-db | — | postgres:16-alpine |
| sessions-db | — | postgres:16-alpine |
| teams-db | — | postgres:16-alpine |
| trivia-db | **5432** | postgres:16-alpine |
| RabbitMQ | 5672, 15672 | rabbitmq:4-management-alpine |

### 15.2 Variables de Entorno Clave
```
Sessions.Service:
  RabbitMq__Host=rabbitmq
  Missions__Url=http://missions.service:80
  RealTimeHub__Url=http://realtimehub:80

Trivia.Service:
  RABBITMQ_HOST=rabbitmq
  RealTimeHub__Url=http://realtimehub:80

Teams.Service:
  Keycloak__BaseUrl=http://keycloak:8080
  Keycloak__AdminUsername=admin
  Keycloak__AdminPassword=admin123
```

---

## 16. RESUMEN EJECUTIVO

### Fortalezas Arquitectónicas
1. ✅ Hexagonal/Clean Architecture consistente en los 4 microservicios
2. ✅ CQRS con MediatR — separación estricta Command/Query
3. ✅ State Pattern para Sessions — transiciones explícitas y testeadas
4. ✅ Chain of Responsibility para validaciones de transición
5. ✅ Composite Pattern en Missions (Mission/Stage/Clue)
6. ✅ JSONB para SessionStage — Value Object sin tabla extra
7. ✅ Compensating Transaction en registro de participantes (Keycloak + DB local)
8. ✅ SignalR con grupos por sesión y reconexión automática
9. ✅ 133 tests con patrones consistentes (AAA, NSubstitute, FluentAssertions)
10. ✅ Frontend con useReducer para estado del juego (sin Redux)

### Debilidades a Resolver
1. 🔴 Consumers RabbitMQ duplicados → consolidar en TypedConsumer
2. 🔴 Scoring cache en memoria → persistir en DB o Redis
3. 🔴 HttpEventPublisher sin retry → Polly o Outbox Pattern
4. 🟡 Sin transacciones distribuidas → Outbox Pattern
5. 🟡 Endpoints internos sin auth → IP whitelist
6. 🟡 Cobertura de tests al 66% → necesita 90% mínimo

### Estimación de Trabajo Restante
- **HU-36 a HU-47** (trivia en vivo): ~3-4 semanas
- **Tests para llegar a 90%**: ~1-2 semanas de test writing
- **Fixes de problemas críticos**: ~3-5 días
- **Pipeline CI/CD**: ~1 día
