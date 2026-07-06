# Architecture Spec — UMBRAL v2

## Estilo Arquitectónico

**Microservicios + Arquitectura Hexagonal (Ports & Adapters) por servicio**

Cada microservicio es independiente: tiene su propia base de datos, su propio proceso Docker, su propia solución .NET y no comparte código con otros servicios.

---

## Servicios y Responsabilidades

| Servicio | Puerto | Responsabilidad única |
|----------|--------|-----------------------|
| `ApiGateway` | 5000 | Punto de entrada único. Enrutamiento YARP + validación JWT Keycloak |
| `Missions.Service` | 5001 | CRUD de misiones, etapas y pistas |
| `Sessions.Service` | 5002 | Ciclo de vida de sesiones, progreso multi-etapa, scoring |
| `Teams.Service` | 5003 | Equipos, miembros, usuarios. Integración con Keycloak Admin API |
| `Trivia.Service` | 5004 | Quizzes, preguntas, respuestas, scoring, leaderboard |
| `RealTimeHub` | 5005 | SignalR hub para broadcasting en tiempo real |

---

## Capa por Servicio: Hexagonal

```
Domain          (centro del hexágono — sin dependencias externas)
  └── Entities, Value Objects, Interfaces, Enums, Domain Events

Application     (orquesta el dominio — solo conoce Domain)
  └── Commands, Queries, Handlers, DTOs, Validators, Interfaces de servicios externos

Infrastructure  (implementa los puertos — conoce todo)
  └── EF Core DbContext, Repositories, RabbitMQ publisher/consumer, Keycloak client, HTTP notifiers

Api             (adaptador de entrada — solo conoce Application)
  └── Minimal API endpoints, Program.cs, DI registration
```

**Regla de dependencia:** Las flechas apuntan hacia adentro. Domain nunca importa Infrastructure. Application nunca importa Infrastructure.

---

## Comunicación entre Servicios

### Síncrona (HTTP directo, para el mismo request-response)
```
ApiGateway ──→ Missions.Service   (enrutamiento de tráfico externo)
ApiGateway ──→ Sessions.Service
Sessions.Service ──→ Missions.Service   (validar que misión existe y está activa)
Sessions.Service ──→ RealTimeHub        (notificar cambios de estado)
Trivia.Service   ──→ RealTimeHub        (notificar preguntas y ranking)
```

### Asíncrona (RabbitMQ, para desacoplamiento)
```
Sessions.Service ──publish──→ trivia.exchange (routing: session.status.changed)
Trivia.Service   ──publish──→ trivia.exchange (routing: answer.submitted)
Trivia.Service   ──consume──← trivia.answer.submitted (actualiza leaderboard)
```

### No existe
- Missions.Service nunca habla con Sessions.Service
- Teams.Service nunca habla con Sessions.Service directamente
- Los servicios no se conocen entre sí por nombre — solo hay referencias a IDs

---

## Datos por Bounded Context

```
Missions DB          Sessions DB          Teams DB
──────────────────   ──────────────────   ──────────────────
Mission              Session              Team
MissionStage           └─ SessionStage[]   └─ TeamMember
MissionClue (JSONB)    └─ SessionParticipant Participant
                     
Trivia DB            Keycloak DB (internal)
──────────────────   ──────────────────────
Quiz                 Users (admin, operator, participant)
Question             Roles
Answer               Attributes: name, alias
LeaderboardEntry     Sessions (SSO tokens)
ParticipantAnswer    
```

**SessionStage es un Value Object guardado como JSONB** — copia desnormalizada de los datos de la misión en el momento de crear la sesión. No hay FK a `missions-db`.

---

## API Gateway (YARP)

El Gateway enruta y autentica. No tiene base de datos ni lógica de negocio.

### Políticas de autorización

| Ruta | Política | Roles permitidos |
|------|---------|-----------------|
| `POST /api/teams/register` | anonymous | Todos |
| `/hub/game/**` | anonymous | Todos (WebSocket) |
| `/api/missions/**` | `operator_or_admin` | operator, admin |
| `/api/sessions/**` | `authenticated` | Cualquier rol autenticado |
| `/api/teams/**` | `authenticated` | Cualquier rol autenticado |
| `/api/quizzes/**` | `operator_or_admin` | operator, admin |
| `/api/admin/operators/**` | `admin` | Solo admin |

### Validación JWT
1. Gateway recibe request con `Authorization: Bearer <token>`
2. Descarga JWKS desde `http://keycloak:8080/realms/umbral/protocol/openid-connect/certs`
3. Valida firma y expiración localmente (sin llamada HTTP por request)
4. Extrae `realm_access.roles` y mapea a `ClaimTypes.Role` via `KeycloakRolesTransformer`
5. Enruta al microservicio correspondiente
6. Los microservicios confían en el Gateway — no re-validan tokens

---

## Patrones de Diseño

| Patrón | Servicio | Propósito |
|--------|---------|-----------|
| **State** | Sessions.Service | Transiciones de estado de Session |
| **CQRS + MediatR** | Todos | Separación Command/Query |
| **Repository** | Todos | Abstracción de persistencia |
| **Strategy** | Trivia.Service | `TimeBasedScoringStrategy` para scoring |
| **Facade** | Sessions.Service | `GameSessionFacade` — transición + notificación |
| **Proxy** | Missions.Service | `MissionAccessProxy` — control de acceso |
| **Chain of Responsibility** | Sessions, Missions | Validaciones de cambio de estado |
| **Template Method** | Sessions.Service | Flujo base de evidencias con variantes |
| **Saga (coreográfica)** | Sessions → Trivia | Coordinación via RabbitMQ sin orquestador central |
| **Outbox** | Sessions.Service | Consistencia eventual para eventos publicados |
| **Database per Service** | Infraestructura | Independencia de datos por bounded context |

---

## Keycloak — Decisión Arquitectónica Clave

Keycloak reemplaza ~500 líneas de código de auth custom (JWT propio, PasswordHasher, TokenService). Beneficios:
- SSO: un login funciona en toda la plataforma
- OIDC + PKCE: estándar de industria
- Revocación de tokens al desactivar operadores
- Admin REST API: crear/deshabilitar usuarios desde Teams.Service
- Roles como Realm Roles en lugar de enum custom

**Trade-off:** Dependencia de infraestructura externa. Si Keycloak cae, nadie puede autenticarse. Mitigación: healthchecks en docker-compose y restart policies.

---

## Decisión: Sessions Multi-Misión

El modelo original tenía `Session.MissionId: Guid` (relación 1:1). Evolucionó a:
- `Session.Stages: List<SessionStage>` (JSONB) — permite múltiples misiones en secuencia
- `Session.CurrentStageOrder: int` — el operador controla el avance
- `SessionStage` copia desnormalizada de datos de la misión (sin FK cross-service)

**Por qué JSONB en vez de tabla relacional:** `SessionStage` es un Value Object cuya identidad pertenece al Aggregate `Session`. No tiene sentido como entidad independiente con su propio ciclo de vida.
