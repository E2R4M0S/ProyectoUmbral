# Backend Spec — UMBRAL v2

## Stack completo

- **.NET 10** con C# 13
- **ASP.NET Core 10** — Minimal APIs (no Controllers MVC)
- **MediatR** — CQRS: `IRequest<T>`, `IRequestHandler<TRequest, TResponse>`
- **FluentValidation** — Validación de Commands con pipeline automático
- **Entity Framework Core 10** — ORM con PostgreSQL via Npgsql
- **NSubstitute** — Mocking en tests (NO Moq)
- **xUnit + FluentAssertions** — Tests unitarios
- **Serilog** — Logging estructurado
- **RabbitMQ.Client** — Messaging asíncrono

---

## Sessions.Service — El más complejo

### Entidades de dominio

```csharp
Session
  ├── Id: Guid
  ├── Name: string
  ├── Pin: string (6 dígitos únicos)
  ├── Status: SessionStatus { Scheduled, Preparing, Active, Paused, Finished, Cancelled }
  ├── CurrentStageOrder: int
  ├── StartedAt: DateTime?
  ├── EndedAt: DateTime?
  ├── CreatedAt: DateTime
  ├── Stages: List<SessionStage>      ← JSONB en PostgreSQL
  └── Participants: List<SessionParticipant>  ← tabla separada

SessionStage (Value Object, en JSONB)
  ├── MissionId: Guid
  ├── MissionTitle: string
  ├── MissionType: string
  └── Order: int

SessionParticipant
  ├── Id: Guid
  ├── SessionId: Guid (FK)
  ├── UserId: Guid (ref Keycloak)
  ├── UserAlias: string
  └── JoinedAt: DateTime
```

### Endpoints implementados

| Método | Ruta | Handler |
|--------|------|---------|
| POST | `/` | `CreateSessionCommand` |
| GET | `/` | `GetSessionsQuery` |
| GET | `/{id}` | `GetSessionByIdQuery` |
| PATCH | `/{id}/status` | `TransitionSessionCommand` |
| POST | `/{id}/join` | `JoinSessionCommand` |
| GET | `/{id}/progress` | `GetSessionProgressQuery` |
| POST | `/{id}/stages/{sid}/advance` | `AdvanceStageCommand` |
| POST | `/{id}/stages/{sid}/clues` | `ReleaseClueCommand` |

### Flujo de transición de estado

```
TransitionSessionCommand
  → Chain: BaseMissionStatusHandler → ValidStatusHandler → NotTerminalHandler
  → session.TransitionTo(newStatus)  ← lógica en la entidad
  → _repo.UpdateAsync(session)
  → _publisher.PublishAsync("session.status.changed", event)
  → _notifier.NotifyStatusChangedAsync(...)  ← HTTP a RealTimeHub
```

---

## Trivia.Service — Flujo de scoring

### Entidades

```csharp
Quiz
  ├── Id: Guid
  └── Title: string

Question
  ├── Id: Guid
  ├── QuizId: Guid (FK)
  ├── Text: string
  ├── TimeLimitSeconds: int
  ├── Order: int
  └── ReleasedAt: DateTime?

Answer
  ├── Id: Guid
  ├── QuestionId: Guid (FK)
  ├── Text: string
  └── IsCorrect: bool

LeaderboardEntry
  ├── Id: Guid
  ├── QuizId: Guid (FK)
  ├── TeamId: Guid
  ├── TeamName: string
  ├── Score: int
  └── UpdatedAt: DateTime

ParticipantAnswer
  ├── Id: Guid
  ├── QuizId: Guid (FK)
  ├── TeamId: Guid
  ├── QuestionId: Guid (FK)
  ├── AnswerId: Guid (FK)
  ├── Timestamp: DateTime
  └── IsCorrect: bool
```

### Flujo completo SubmitAnswer

```
POST /answers (SubmitAnswerCommand)
  → TimeBasedScoringStrategy.Calculate(timeLimitSeconds, responseTime)
  → Guardar ParticipantAnswer
  → Publicar TriviaAnswerSubmittedEvent a RabbitMQ (routing: answer.submitted)
  → Retornar { isCorrect, pointsAwarded }
  
↓ (asíncrono, desacoplado)

TriviaAnswerSubmittedConsumer (BackgroundService)
  → Consume de cola trivia.answer.submitted
  → UpdateLeaderboardCommand → actualizar LeaderboardEntry
  → POST /internal/events/LeaderboardUpdated → RealTimeHub
  → SignalR RankingUpdated → todos los clientes
```

### Endpoints

| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/quizzes` | Crear quiz |
| GET | `/quizzes` | Listar quizzes |
| GET | `/quizzes/{id}` | Detalle quiz |
| POST | `/start-trivia` | Iniciar partida de trivia |
| POST | `/questions/ask` | Lanzar pregunta a todos |
| GET | `/questions/{id}/answer-count` | Cuántos respondieron |
| POST | `/answers` | Enviar respuesta (participante) |
| POST | `/games/{sessionId}/end` | Finalizar trivia |
| GET | `/ranking` | Leaderboard actual |

---

## Teams.Service — Keycloak Admin API

### Integración con Keycloak

```csharp
// IKeycloakAdminClient (interfaz en Domain)
public interface IKeycloakAdminClient
{
    Task<string> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default);
    Task DisableUserAsync(string keycloakUserId, CancellationToken ct = default);
    Task UpdateUserAttributesAsync(string keycloakUserId, Dictionary<string, string> attributes, CancellationToken ct = default);
    Task<List<UserRepresentation>> GetUsersAsync(string? search = null, string? role = null, CancellationToken ct = default);
}

// Implementación usa:
// - Token de servicio via Client Credentials (cliente umbral-gateway)
// - POST /admin/realms/umbral/users
// - PUT /admin/realms/umbral/users/{id}
// - GET /admin/realms/umbral/users?search=...&role=...
```

### Entidades locales

```csharp
Team          → Id, Name, Description, LeaderId(Keycloak), JoinCode, CreatedAt
TeamMember    → TeamId, UserId(Keycloak)
Participant   → Id, Name, Alias, Email, KeycloakUserId, CreatedAt
```

`Participant` guarda una copia local del usuario de Keycloak (para queries rápidas sin llamar a Keycloak).

---

## Missions.Service

### Entidades

```csharp
Mission       → Id, Title, Description, Difficulty(Easy/Medium/Hard), TimeMinutes, Type(Treasure/Trivia), Status(Draft/Active/Inactive)
MissionStage  → Id, MissionId, Name, Description, Order
MissionClue   → Id, StageId, Content, Penalty(int?), ReleaseType(Auto/Manual)
```

### Reglas de activación

- Status pasa de `Draft` → `Active` solo si tiene al menos 1 etapa (`DraftToActiveRequiresStagesHandler`)
- Bloquear edición/desactivación si está en uso en sesión activa (llamada HTTP síncrona a Sessions.Service)

---

## Configuración de un nuevo microservicio

```csharp
// Program.cs template
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()          // MediatR + Validators
    .AddInfrastructure(builder.Configuration);  // DbContext + Repos + RabbitMQ

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("admin", p => p.RequireRole("admin"));
    options.AddPolicy("operator_or_admin", p => p.RequireRole("operator", "admin"));
    options.AddPolicy("authenticated", p => p.RequireAuthenticatedUser());
});

var app = builder.Build();

// EnsureCreated
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();
    db.Database.EnsureCreated();
}

app.MapMyEndpoints();
app.Run();
```
