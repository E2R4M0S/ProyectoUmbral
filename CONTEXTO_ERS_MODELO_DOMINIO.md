# UMBRAL — Contexto de Reglas de Negocio y Modelo de Dominio

> Documento de trabajo generado a partir de una auditoría del código fuente real (no solo de `CLAUDE.md`, que en varios puntos quedó desactualizado respecto a la implementación). Su propósito es servir de **input único y confiable** para que otro asistente (Claude Desktop) redacte un **ERS (Especificación de Requerimientos de Software)** formal y un **modelo de dominio / diagrama de clases** profesional para el proyecto UMBRAL.
>
> Todo lo aquí descrito fue verificado leyendo entidades de dominio, handlers de aplicación, configuración del API Gateway y rutas reales — no son suposiciones. Donde el diseño original (documentado en `CLAUDE.md`) divergió de lo implementado, se marca explícitamente con **⚠️ DIVERGENCIA**.

---

## 1. Identidad del sistema

**UMBRAL** es una plataforma web para operar en tiempo real experiencias de investigación inmersiva tipo "escape room académico", desarrollada para la UCAB (materia Desarrollo de Software).

Un **Operador** crea una **sesión de juego** a partir de una o más **misiones** del catálogo (Búsqueda del Tesoro y/o Trivia). Los **participantes** se unen a la sesión con un **PIN**, se organizan en **equipos formados dentro de la propia sesión**, y juegan en tiempo real vía WebSockets (SignalR): escaneando códigos QR geolocalizados (Tesoro) y/o respondiendo preguntas cronometradas (Trivia). El sistema calcula puntajes, aplica bonos de podio, lleva un registro de auditoría trazable y cierra con un ranking final.

**Equipo:** Eros Dos Ramos y Adrián Cereijo — .NET 10 + React 19 + Keycloak + PostgreSQL + RabbitMQ + SignalR, arquitectura de microservicios hexagonal.

---

## 2. Arquitectura real actual

```
┌───────────────────────────────────────────────────────────────┐
│                     API Gateway (YARP) — :5000                 │
│         Validación JWT (Keycloak) + enrutamiento por policy    │
└───────┬───────────────┬───────────────┬─────────────────────────┘
        │               │               │
   ┌────▼────┐   ┌──────▼──────┐   ┌────▼─────┐   ┌──────────────┐
   │Missions │   │  Sessions   │   │  Trivia  │   │  RealTimeHub  │
   │ Service │   │  Service    │   │ Service  │   │  (SignalR)    │
   │ :5001   │   │  :5002      │   │ :5004    │   │  :5005        │
   └─────────┘   └─────────────┘   └──────────┘   └──────────────┘
        │               │               │
   missions-db     sessions-db      trivia-db
```

### ⚠️ DIVERGENCIA — Teams.Service ya no existe

El diseño original documentaba un **cuarto microservicio, Teams.Service**, con equipos preformados antes de la sesión (líder + `JoinCode`, HU-15 a HU-19). **Esto fue eliminado del sistema.** Verificado en:

- No existe la carpeta `src/Teams.Service/`.
- El API Gateway (`appsettings.json`) **no tiene ninguna ruta `/api/teams`** — solo existen `missions`, `sessions`, `trivia`, `quizzes`, `admin-*`, `register`, `profile`, `health`, `realtime-hub`.
- `register` y `profile` (antes responsabilidad de Teams.Service) ahora enrutan a **missions-cluster**. La gestión de usuarios de Keycloak (`IKeycloakAdminService`) vive en `Missions.Infrastructure`.
- El frontend conserva un archivo legado `frontend/src/services/teamsApi.ts` que llama a `/api/teams/*` — **es código muerto**, no tiene backend detrás. El archivo activo es `frontend/src/services/sessionTeamsApi.ts`, que llama a `/api/sessions/{sessionId}/teams/*`.

**Regla de negocio real:** los equipos **ya no se crean fuera de una sesión**. Se crean y se unen participantes **dentro de una sesión concreta** (`SessionTeam` vive en `Sessions.Service`, no en un servicio de equipos independiente). No hay `JoinCode` de equipo — la membresía se gestiona por endpoints propios de la sesión (`POST /api/sessions/{sessionId}/teams`, `POST /api/sessions/{sessionId}/teams/{teamId}/join`).

Los 3 microservicios de negocio reales son: **Missions.Service**, **Sessions.Service**, **Trivia.Service**, más el **RealTimeHub** (sin persistencia) y el **API Gateway**.

---

## 3. Actores y roles (sin cambios respecto al diseño original)

| Actor | Rol Keycloak | Responsabilidades |
|---|---|---|
| Administrador | `admin` | Gestiona operadores, catálogo de misiones y quizzes, supervisión global (solo lectura sobre sesiones) |
| Operador | `operator` | Crea y opera sus propias sesiones en vivo; gestiona equipos y pistas de esas sesiones |
| Participante | `participant` | Se registra, se une a sesiones con PIN, forma/se une a equipos dentro de la sesión, juega (QR / trivia), consulta resultados |

Políticas reales del Gateway: `authenticated` (sesiones, trivia), `operator_or_admin` (misiones, quizzes, admin-users), `admin` (admin-operators, admin-missions), `anonymous` (health, register, hub SignalR).

---

## 4. Modelo de dominio detallado (verificado en código, campo por campo)

### 4.1 Bounded Context: Missions.Service

```
Mission
├─ Id: Guid
├─ Title: string (único)
├─ Description: string
├─ Difficulty: enum { Easy, Medium, Hard }   // Missions.Domain.Enums.Difficulty
├─ TimeMinutes: int
├─ Type: enum { Treasure, Trivia }           // MissionType
├─ Status: enum { Draft, Active, Inactive }  // MissionStatus
├─ CreatedAt: DateTime
└─ Stages: List<MissionStage>  (1..*, agregado raíz — todo cambio de stage/clue pasa por Mission)

MissionStage
├─ Id: Guid
├─ MissionId: Guid (FK)
├─ Name: string (único dentro de la misión, case-insensitive)
├─ Description: string
├─ Order: int (único dentro de la misión)
├─ QrToken: string            // generado automáticamente (Guid sin guiones) al crear la etapa
├─ Latitude: double?          // ubicación GPS opcional (selector de mapa en el frontend)
├─ Longitude: double?
└─ Clues: List<MissionClue> (requiere mínimo 1 para que la etapa sea válida)

MissionClue (implementa IMissionComponent — patrón Composite)
├─ Id: Guid
├─ StageId: Guid (FK)
├─ Content: string
├─ Penalty: int?              // puntos a descontar si se libera esta pista
└─ ReleaseType: enum { Automatic, Manual }   // ReleaseType

Enums:
  Difficulty     { Easy, Medium, Hard }
  MissionType    { Treasure, Trivia }
  MissionStatus  { Draft, Active, Inactive }
  ReleaseType    { Automatic, Manual }
```

**Patrón Composite real:** `Mission`, `MissionStage` y `MissionClue` implementan `IMissionComponent` con `Validate()`, `GetTotalPenalty()` y `GetLeafCount()` — cada nivel agrega recursivamente el del nivel inferior. Esto formaliza el patrón "Composite" que el `CLAUDE.md` no documentaba explícitamente.

**Reglas de validación reales (`Mission.Validate()` / `MissionStage.Validate()`):**
- Una misión requiere al menos 1 etapa; una etapa requiere al menos 1 pista.
- No puede haber dos etapas con el mismo `Order` ni el mismo `Name` (case-insensitive) dentro de la misma misión.
- `MissionStatus.Draft` no puede pasar directo a `Inactive` (solo `Draft → Active`, o `Active ⇄ Inactive`).

### 4.2 Bounded Context: Sessions.Service

```
Session (agregado raíz)
├─ Id: Guid
├─ Name: string
├─ Pin: string (6 dígitos, único)
├─ Stages: List<SessionStage>          // snapshot inmutable de las misiones al momento de crear la sesión
├─ CurrentStageOrder: int
├─ Status: SessionStatus
├─ OperatorId: Guid?                   // RB-10 — ver más abajo
├─ StartedAt / EndedAt: DateTime?
├─ CurrentMissionStartedAt: DateTime?  // ancla real de cuándo arrancó la misión actual (no un cálculo acumulado de duraciones estimadas)
├─ CreatedAt: DateTime
└─ Participants: List<SessionParticipant>

SessionStage (Value Object embebido, snapshot de una Mission+MissionStage al crear la sesión)
├─ MissionId: Guid
├─ MissionStageId: Guid
├─ MissionTitle: string
├─ StageName: string
├─ MissionType: string ("Treasure" | "Trivia")
├─ Order: int
├─ QrToken: string             // copiado de MissionStage al crear la sesión
├─ TimeMinutes: int
├─ Latitude / Longitude: double?
├─ Difficulty: string ("Easy" | "Medium" | "Hard", default "Medium")
└─ BaseScanPoints (calculado): Easy=100, Medium=150, Hard=200

SessionTeam                                  // equipo formado DENTRO de la sesión
├─ Id: Guid
├─ SessionId: Guid (FK)
├─ Name: string (máx. 100 caracteres)
├─ MaxMembers: int = 5 (fijo)
├─ Score: int
├─ LastScoreAt: DateTime?      // RB-08 — desempate de ranking
├─ CreatedAt: DateTime
└─ Members: List<SessionTeamMember>

SessionTeamMember
├─ Id: Guid
├─ SessionTeamId: Guid (FK)
├─ UserId: Guid (ref. Keycloak)
├─ UserAlias: string
└─ JoinedAt: DateTime

SessionParticipant                           // todo usuario que se une a la sesión (esté o no en un equipo)
├─ Id: Guid
├─ SessionId: Guid (FK)
├─ UserId: Guid (ref. Keycloak)
├─ UserAlias: string
├─ JoinedAt: DateTime
├─ CurrentStageOrder: int      // progreso individual en el recorrido de etapas Treasure
├─ CompletedAt: DateTime?
├─ IsWaitingAtGate: bool       // participante que ya llegó a una compuerta de sincronización (p.ej. esperando a su equipo)
├─ Score: int                  // puntaje individual (participantes sin equipo puntúan por su cuenta)
└─ LastScoreAt: DateTime?

SessionAuditEvent                            // RB-06 / RB-07 — trazabilidad completa
├─ Id: Guid
├─ SessionId: Guid (FK)
├─ EventType: string { PenaltyApplied, EvidenceValidated, EvidenceRejected,
│                       StatusChanged, TriviaAnswerScored, AutomaticClueReleased }
├─ Description: string
├─ TeamId: Guid?
├─ UserId: Guid?
├─ ClueId: Guid?               // liga el evento a la pista concreta (dedupe de liberaciones automáticas)
├─ ScoreDelta: int?
└─ OccurredAt: DateTime

Enum:
  SessionStatus { Scheduled, Preparing, Active, Paused, Finished, Cancelled }
```

**Máquina de estados real de `Session` (State Pattern, sin cambios respecto al diseño):**
```
Scheduled → Preparing → Active ⇄ Paused
                              ↘ Finished
(cualquier estado no terminal) → Cancelled
Finished / Cancelled = terminales
```

### 4.3 Bounded Context: Trivia.Service

```
Quiz
├─ Id: Guid
└─ Title: string

Question
├─ Id: Guid
├─ QuizId: Guid (FK)
├─ Text: string?
├─ TimeLimitSeconds: int
└─ ReleasedAt: DateTime?      // marca cuándo se hizo POST /questions/ask

Answer
├─ Id: Guid
├─ QuestionId: Guid (FK)
├─ Text: string
└─ IsCorrect: bool

LeaderboardEntry
├─ Id: Guid
├─ QuizId: Guid (FK)
├─ TeamId: Guid (ref. externo a SessionTeam)
├─ TeamName: string?
├─ Score: int
└─ UpdatedAt: DateTime

ParticipantAnswer
├─ Id: Guid
├─ QuizId: Guid (FK)
├─ TeamId: Guid (ref. externo)
├─ QuestionId: Guid (FK)
├─ AnswerId: Guid (FK)
├─ Timestamp: DateTime
└─ IsCorrect: bool
```

**Nota de implementación real:** el scoring y el ranking de Trivia.Service están **a nivel de equipo (`TeamId`)**, no de participante individual — a diferencia de Sessions.Service, donde tanto equipos como participantes sueltos tienen `Score` propio. `QuizId` se reutiliza como `SessionId` (mismo valor) para correlacionar la partida de trivia con la sesión que la contiene.

### 4.4 Referencias entre bounded contexts

```
Sessions.SessionStage.MissionId       ─ ref →  Missions.Mission.Id
Sessions.SessionStage.MissionStageId  ─ ref →  Missions.MissionStage.Id
Trivia.LeaderboardEntry.TeamId        ─ ref →  Sessions.SessionTeam.Id
Trivia.ParticipantAnswer.TeamId       ─ ref →  Sessions.SessionTeam.Id
Sessions.Session.OperatorId           ─ ref →  Keycloak User.Id (rol operator)
Sessions.SessionParticipant.UserId    ─ ref →  Keycloak User.Id (rol participant)
```

No hay foreign keys de base de datos entre servicios (cada uno con su propia PostgreSQL) — todas las referencias cruzadas son lógicas y se resuelven vía llamadas HTTP internas o eventos.

---

## 5. Reglas de negocio consolidadas (verificadas en código)

| Código | Regla | Fuente / evidencia |
|---|---|---|
| RB-01 | Una misión solo puede usarse en una sesión si está en estado `Active`. | Diseño original, vigente |
| RB-02 | Una etapa requiere mínimo 1 pista y una misión mínimo 1 etapa para poder validarse/activarse. | `Mission.Validate()`, `MissionStage.Validate()` |
| RB-03 | No se aceptan respuestas de trivia ni evidencias QR si la sesión no está `Active` (bloqueadas explícitamente en `Paused`, `Finished`, `Cancelled`). | `ValidateQrCommandHandler` (chequea `Status != Active`), `SubmitAnswerCommandHandler.GetBlockedSessionStatusAsync` (consulta cross-service a Sessions.Service; **fail-open** si Sessions.Service no responde, para no bloquear el juego en vivo por un fallo de red transitorio) |
| RB-04 | Una pista no se libera dos veces al mismo equipo para la misma etapa. | `ClueId` en `SessionAuditEvent` permite deduplicar liberaciones automáticas |
| RB-05 | Cada evidencia (QR) queda asociada a un equipo (o participante suelto), sesión y etapa concretos. | `SessionAuditEvent.TeamId/UserId/ClueId`, evento `evidence.submitted` en RabbitMQ |
| RB-06 | Toda penalización y todo evento relevante de la partida (pista liberada, evidencia válida/rechazada, cambio de estado, puntaje de trivia) registra motivo, monto y momento. | `SessionAuditEvent` con `EventType`, `Description`, `ScoreDelta`, `OccurredAt` — es un **log de auditoría de dominio real**, no solo un concepto de diseño |
| RB-07 | El puntaje acumulado tiene trazabilidad de origen (pista usada, escaneo QR, bono de podio, acierto de trivia). | Cada `AddScore`/`ApplyPenalty` en `SessionTeam`/`SessionParticipant` va acompañado de un `SessionAuditEvent` con `ScoreDelta` |
| RB-08 | El ranking se ordena por puntaje descendente; el momento del último cambio de puntaje (`LastScoreAt`) actúa como desempate (llega antes = mejor posición). | `SessionTeam.LastScoreAt`, `SessionParticipant.LastScoreAt` |
| RB-09 | Los cambios de estado de sesión respetan las transiciones válidas del State Pattern; no se puede volver a fijar el mismo estado (lanza excepción). | `Session.TransitionTo()` |
| **RB-10** | **Cada sesión tiene un `OperatorId` (quien la creó). Solo ese operador puede administrarla** (iniciar, pausar, finalizar, liberar pistas, avanzar etapa). Una sesión sin `OperatorId` registrado (creada antes de existir esta regla) **no puede ser administrada por nadie** — no se abre por defecto. El Administrador tiene **solo lectura** sobre las sesiones (supervisión), no puede operarlas. | `Session.OperatorId`, `Session.IsManagedBy(userId)` — comentario explícito en el código: *"the operator who created this session is the only one who may manage it"* |
| RB-11 | Los equipos se forman **dentro de la sesión** (no existen antes de ella); un equipo tiene un máximo fijo de 5 miembros; un usuario no puede unirse dos veces al mismo equipo. | `SessionTeam.Create/AddMember`, `MaxMembers = 5` |
| RB-12 | Cuando un miembro de un equipo escanea un QR válido, **el avance de etapa y el puntaje se comparten con todo el equipo** — no hace falta que cada miembro escanee. | `ValidateQrCommandHandler`: al validar, hace `CatchUpTo` sobre los compañeros de equipo y acredita el puntaje al equipo, no al individuo |
| RB-13 | El puntaje base de cada escaneo QR válido depende de la dificultad de la misión: Fácil = 100, Media = 150, Difícil = 200. | `SessionStage.BaseScanPoints` |
| RB-14 | Al completar todas las etapas de la parte Treasure, se otorga un **bono de podio** por orden de llegada (a nivel de grupo — equipo o participante suelto): 1er lugar +300, 2do +200, 3er +100, del 4to en adelante +0. | `ValidateQrCommandHandler` (bloque de `bonus`) |
| RB-15 | La sesión avanza de etapa automáticamente cuando **todos** los participantes conectados superaron la etapa actual (sin esperar un clic manual del operador); si además todos completaron la última etapa, la sesión se finaliza automáticamente. | `AutoAdvanceSessionStageIfEveryoneCaughtUp`, chequeo `allDone` en `ValidateQrCommandHandler` |
| RB-16 | En Trivia, cada participante responde como máximo una vez por pregunta (idempotencia por usuario), y solo la **primera** respuesta del equipo dispara el cálculo de puntaje y la notificación en tiempo real (idempotencia por equipo) — respuestas posteriores del mismo equipo se registran pero no puntúan de nuevo. | `SubmitAnswerCommandHandler` (`UserAnswers`, `TeamAnswers` con `TryAdd`) |
| RB-17 | El puntaje de trivia es por tiempo de respuesta, no binario: 40 pts si responde en el primer 25% del tiempo límite, 30 en el segundo cuarto, 20 en el tercero, 10 en el último cuarto o fuera de tiempo (siempre que la respuesta sea correcta). | `TimeBasedScoringStrategy.CalculateScore` |
| RB-18 | El puntaje de un equipo ganado en Trivia se sincroniza de vuelta hacia Sessions.Service (para que el ranking consolidado de la sesión lo refleje), vía llamada HTTP interna. | `SubmitAnswerCommandHandler` → `POST /internal/teams/score` en Sessions.Service |

**Nota sobre el `CLAUDE.md` del repositorio:** el archivo tiene actualmente un cambio local sin commitear que **revierte** RB-10 a "cualquier operador administra cualquier sesión" — esto **contradice el código real** (`Session.OperatorId` / `IsManagedBy`), que sí implementa ownership por operador. Al construir el ERS, priorizar lo verificado en este documento sobre el texto de `CLAUDE.md`.

---

## 6. Flujos críticos (para que el ERS los describa como casos de uso)

### 6.1 Alta y ejecución de una sesión mixta (Treasure + Trivia)
1. Operador crea una sesión con 1+ misiones (`POST /api/sessions`), quedando registrado como `OperatorId`.
2. Se genera un PIN de 6 dígitos único; la sesión queda `Scheduled` → el operador la pasa a `Preparing`.
3. Participantes se unen con el PIN (`SessionParticipant`), forman o se unen a `SessionTeam` dentro de la sesión.
4. Operador inicia (`Active`); arranca `CurrentMissionStartedAt` para la primera etapa.
5. Si la etapa es **Treasure**: los participantes escanean el QR geolocalizado de la etapa actual; validación, puntaje por dificultad, catch-up de equipo, posible bono de podio, auto-avance cuando todos terminan.
6. Si la etapa es **Trivia**: el operador lanza preguntas (`POST /questions/ask`), los equipos responden contrarreloj, puntaje por rapidez, ranking en tiempo real vía SignalR, sincronizado de vuelta a Sessions.Service.
7. Al agotarse las etapas o cuando todos completan, la sesión pasa a `Finished` (automático o manual) y se congela el ranking.

### 6.2 Auditoría y trazabilidad
Cada evento relevante de la partida (pista liberada, evidencia válida/inválida, penalización, cambio de estado, puntaje de trivia) genera un `SessionAuditEvent` persistente — este es el mecanismo real detrás de RB-06/RB-07, y debería documentarse como requerimiento explícito en el ERS ("el sistema debe mantener un registro de auditoría de toda la partida").

---

## 7. Eventos de integración (reales, verificados)

| Evento | Origen → Destino | Transporte | Payload relevante |
|---|---|---|---|
| `evidence.submitted` | Sessions.Service → RabbitMQ | Best-effort (no bloquea el flujo si falla) | SessionId, UserId, TeamId, StageId, IsValid, ScoreDelta |
| `session.status.changed` | Sessions.Service → RabbitMQ (`trivia.exchange`) | — | sessionId, newStatus |
| `TriviaAnswerSubmittedEvent` | Trivia.Service → RabbitMQ (`trivia.answer.submitted`) | Consumida por `TriviaAnswerSubmittedConsumer` | answer completo |
| `POST /internal/teams/score` | Trivia.Service → Sessions.Service (HTTP directo) | Sincroniza puntaje de trivia hacia el equipo de la sesión | TeamId, Delta |
| `POST /internal/notifications/team-answer-submitted` | Trivia.Service → RealTimeHub | Notifica a los miembros del equipo su resultado inmediato | SessionId, TeamId, QuestionId, IsCorrect, PointsAwarded |
| `POST /internal/events/LeaderboardUpdated` | Trivia.Service → RealTimeHub → SignalR `RankingUpdated` | Ranking de trivia recalculado | leaderboard completo |
| `NotifyRankingUpdatedAsync` / `NotifyTeamStageAdvancedAsync` | Sessions.Service → RealTimeHub → SignalR | Ranking y avance de etapa Treasure en tiempo real | — |

---

## 8. Patrones de diseño confirmados en código

| Patrón | Evidencia real |
|---|---|
| **State** | `ISessionState`, `ScheduledState`, `PreparingState`, `ActiveState`, `PausedState`, `FinishedState`, `CancelledState`, `StateFactory` |
| **Composite** | `IMissionComponent` implementado por `Mission`, `MissionStage`, `MissionClue` con `Validate()/GetTotalPenalty()/GetLeafCount()` recursivos |
| **Strategy** | `IScoringStrategy` / `TimeBasedScoringStrategy` en Trivia.Service |
| **CQRS (MediatR)** | Un `Command`/`Query` + `Handler` por caso de uso en cada servicio |
| **Repository** | Interfaces en `*.Application/Common/Interfaces`, implementación EF Core en `*.Infrastructure` |
| **Facade** | `IGameSessionFacade` (Sessions.Service) coordina transición de estado + notificación en un solo paso |
| **Proxy** | `MissionAccessProxy` — control de acceso por rol antes de delegar al repositorio real |
| **Chain of Responsibility** | `IMissionStatusHandler` / `BaseMissionStatusHandler` (validación de cambio de estado de misión) |
| **API Gateway** | YARP con políticas de autorización por ruta |
| **Database per Service** | `missions-db`, `sessions-db`, `trivia-db` |
| **Audit Log / Event Sourcing ligero** | `SessionAuditEvent` — no estaba en la lista original de patrones documentados, pero es un patrón real y central del dominio |

---

## 9. Requerimientos no funcionales (sin cambios sustanciales)

- .NET 10 + React 19 + PostgreSQL 16 + SignalR + RabbitMQ + Keycloak (OIDC/PKCE) + YARP.
- Arquitectura hexagonal por servicio (Domain sin dependencias de Infrastructure).
- CQRS estricto con MediatR + FluentValidation.
- `EnsureCreated()` (sin migraciones EF Core) — cambios de esquema requieren recrear el contenedor de esa base.
- Cobertura de tests backend objetivo ≥ 90% (xUnit + FluentAssertions + NSubstitute).
- Sin librerías de UI externas en frontend (CSS plano).

---

## 10. Qué corregir respecto al `CLAUDE.md` original al redactar el ERS

1. **No modelar Teams.Service como microservicio independiente.** Los equipos son un sub-agregado de `Session` (bounded context Sessions.Service).
2. **RB-10 es ownership real por operador**, no "cualquier operador administra cualquier sesión".
3. **La ejecución de trivia en vivo (HU-36 a HU-44 del documento original) ya está sustancialmente implementada** end-to-end (pregunta → respuesta cronometrada → puntaje → ranking en tiempo real → sincronización cruzada con Sessions.Service), con mecanismos de idempotencia por usuario y por equipo que el documento original no anticipaba.
4. **Añadir el modelo Treasure con QR + geolocalización** (`MissionStage.QrToken/Latitude/Longitude`, `SessionStage.BaseScanPoints`, bono de podio, auto-avance de etapa) como flujo de negocio de primer nivel — es más rico que "liberar pistas manualmente".
5. **Incorporar `SessionAuditEvent` como entidad de dominio de primera clase** (auditoría/trazabilidad), no solo como un requerimiento no funcional implícito.
6. Un participante puede jugar **sin equipo** (puntaje individual) — el modelo no obliga a que todo participante pertenezca a un `SessionTeam`.

---

## 11. Prompt sugerido para Claude Desktop

Copiar y pegar esto junto con este documento (adjunto o pegado completo) en una conversación nueva de Claude Desktop:

```
Eres un analista de requerimientos y arquitecto de software senior. Adjunto un documento
("Contexto de Reglas de Negocio y Modelo de Dominio") que describe, verificado directamente
contra el código fuente, el estado REAL del sistema UMBRAL (plataforma de escape room
académico con microservicios .NET, React, Keycloak, SignalR y RabbitMQ).

Con base ÚNICAMENTE en ese documento (no inventes funcionalidad que no esté ahí, y señala
explícitamente si detectas alguna inconsistencia interna en el propio documento), quiero que
generes dos entregables profesionales y listos para entregar como parte de un proyecto
académico de Ingeniería de Software:

1. UN ERS (Especificación de Requerimientos de Software) completo, en español, siguiendo
   una estructura estándar tipo IEEE 830 / ISO 29148 adaptada:
   - Introducción (propósito, alcance, definiciones/acrónimos, referencias)
   - Descripción general (perspectiva del producto, funciones, características de usuarios,
     restricciones, supuestos y dependencias)
   - Requerimientos específicos:
       - Requerimientos funcionales, organizados por módulo/bounded context, redactados como
         Historias de Usuario con criterios de aceptación verificables (dado-cuando-entonces
         o listas numeradas), y derivados de las reglas de negocio (RB-01 a RB-18) y flujos
         críticos del documento adjunto.
       - Requerimientos no funcionales (rendimiento, seguridad, disponibilidad, usabilidad,
         mantenibilidad), basados en la sección 9 del documento.
       - Reglas de negocio como sección propia, tabuladas y trazables a los requerimientos
         funcionales que las usan.
   - Casos de uso principales (al menos: crear sesión, unirse a sesión y formar equipo,
     ejecutar etapa Treasure con QR, ejecutar ronda de Trivia, cerrar sesión y ver ranking),
     con diagramas de secuencia en texto/mermaid si es útil.

2. UN MODELO DE DOMINIO profesional (diagrama de clases), en formato Mermaid (classDiagram),
   que represente fielmente las entidades, atributos, tipos, enums, relaciones (1-1, 1-N,
   referencias cruzadas entre bounded contexts) y métodos de negocio relevantes descritos en
   la sección 4 del documento — respetando los bounded contexts (Missions, Sessions, Trivia)
   como paquetes/namespaces separados, y marcando con una nota las referencias cruzadas sin
   integridad referencial de base de datos.

Reglas para tu trabajo:
- Prioriza siempre lo verificado en el documento adjunto sobre cualquier conocimiento previo
  o suposición sobre "cómo debería ser" un sistema de este tipo.
- Si el documento marca una "⚠️ DIVERGENCIA" o una sección "10. Qué corregir", asegúrate de
  que el ERS final refleje la versión corregida, no el diseño original superado.
- Sé exhaustivo pero no inventes campos, entidades o reglas que no estén en el documento.
- Entrega el ERS en Markdown bien estructurado y el modelo de dominio en un bloque ```mermaid```
  autocontenido y sintácticamente válido.
```

---

*Documento generado el 2026-07-17 mediante auditoría directa del código fuente del repositorio (no solo de la documentación existente).*
