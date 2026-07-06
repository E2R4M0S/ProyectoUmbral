---
description: Estándares de código C# y TypeScript para UMBRAL. Siempre activos.
alwaysApply: true
---

# Coding Standards — UMBRAL v2

## C# / .NET

### Nomenclatura
```csharp
// Clases, métodos, propiedades públicas → PascalCase
public class CreateMissionCommandHandler { }
public Guid MissionId { get; private set; }

// Variables locales, parámetros → camelCase
var missionId = command.MissionId;
public Task Handle(CreateMissionCommand command, ...)

// Interfaces → prefijo I
public interface IMissionRepository { }

// Enums → PascalCase, valores PascalCase
public enum SessionStatus { Scheduled, Preparing, Active, Paused, Finished, Cancelled }
```

### Un archivo por clase, nombrado igual que la clase
```
CreateMissionCommand.cs       → public class CreateMissionCommand
CreateMissionCommandHandler.cs → public class CreateMissionCommandHandler
IMissionRepository.cs          → public interface IMissionRepository
```

### Pattern Command (MediatR)
```csharp
// Command
public record CreateMissionCommand(string Title, string Description, ...) : IRequest<CreateMissionResult>;

// Result (solo lo necesario — no entidades completas)
public record CreateMissionResult(Guid MissionId);

// Handler
public class CreateMissionCommandHandler : IRequestHandler<CreateMissionCommand, CreateMissionResult>
{
    private readonly IMissionRepository _repo;
    public CreateMissionCommandHandler(IMissionRepository repo) => _repo = repo;

    public async Task<CreateMissionResult> Handle(CreateMissionCommand command, CancellationToken ct)
    {
        // 1. Construir entidad de dominio
        var mission = Mission.Create(command.Title, ...);
        // 2. Persistir
        await _repo.AddAsync(mission, ct);
        // 3. Retornar
        return new CreateMissionResult(mission.Id);
    }
}

// Validator (FluentValidation)
public class CreateMissionCommandValidator : AbstractValidator<CreateMissionCommand>
{
    public CreateMissionCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
    }
}
```

### Pattern Query (MediatR)
```csharp
public record GetMissionByIdQuery(Guid MissionId) : IRequest<MissionDetailDto?>;

public class GetMissionByIdQueryHandler : IRequestHandler<GetMissionByIdQuery, MissionDetailDto?>
{
    // Queries pueden ir directo al DbContext, sin pasar por el repositorio de dominio
    private readonly MissionsDbContext _db;
    public GetMissionByIdQueryHandler(MissionsDbContext db) => _db = db;

    public async Task<MissionDetailDto?> Handle(GetMissionByIdQuery query, CancellationToken ct)
    {
        var mission = await _db.Missions.FindAsync(query.MissionId, ct);
        return mission is null ? null : new MissionDetailDto(mission.Id, mission.Title, ...);
    }
}
```

### Entidades de dominio
```csharp
public class Mission
{
    // Constructor privado — solo se crea con factory methods
    private Mission() { }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = null!;

    // Colecciones privadas, expuestas como IReadOnlyList
    private readonly List<MissionStage> _stages = new();
    public IReadOnlyList<MissionStage> Stages => _stages.AsReadOnly();

    // Factory method
    public static Mission Create(string title, string description, ...)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        return new Mission { Id = Guid.NewGuid(), Title = title, ... };
    }

    // Métodos de dominio (comportamiento, no setters públicos)
    public void AddStage(string name, int order) { ... }
    public void Activate() { ... }
}
```

### Tests — xUnit + FluentAssertions + NSubstitute
```csharp
// Nombre: Given_When_Then
public class Given_CreateMissionCommand_When_TitleIsEmpty_Then_ValidationFails
{
    [Fact]
    public async Task Handle_Should_ThrowValidationException_When_TitleIsEmpty()
    {
        // Arrange
        var repo = Substitute.For<IMissionRepository>();
        var handler = new CreateMissionCommandHandler(repo);
        var command = new CreateMissionCommand("", "desc", ...);

        // Act
        var act = () => handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        await repo.DidNotReceive().AddAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>());
    }
}
```

### Comentarios
Solo si el WHY no es obvio. No describir QUÉ hace el código.
```csharp
// PKCE desactivado: crypto.subtle requiere HTTPS, no disponible en dev HTTP
options.DisablePkce = true;

// NO escribir esto:
// Obtiene la misión por su ID
var mission = await _repo.GetByIdAsync(id);
```

---

## TypeScript / React

### Nomenclatura
```tsx
// Componentes → PascalCase
export function CrearSesion() { }
export const QuestionCard: React.FC<Props> = () => { }

// Hooks → camelCase con prefijo "use"
export function useSignalR(sessionId: string) { }

// Variables, funciones → camelCase
const sessionId = params.id;
async function fetchSessions() { }

// Tipos e interfaces → PascalCase
interface MissionDto { id: string; title: string; }
type SessionStatus = 'Scheduled' | 'Preparing' | 'Active' | 'Paused' | 'Finished' | 'Cancelled';
```

### Componentes funcionales con tipos explícitos
```tsx
interface Props {
  sessionId: string;
  onStatusChange: (status: SessionStatus) => void;
}

export function PanelSesion({ sessionId, onStatusChange }: Props) {
  const [session, setSession] = useState<SessionDto | null>(null);
  // ...
}
```

### Servicios API
```tsx
// Siempre en services/<dominio>Api.ts
// Exportar funciones async que retornan tipos explícitos

export async function createSession(data: CreateSessionRequest): Promise<SessionDto> {
  const res = await api.post<SessionDto>('/api/sessions', data);
  return res.data;
}
```

### Sin librerías de UI externas
```tsx
// Correcto: inline styles o CSS plano
<button style={{ backgroundColor: '#2563eb', color: 'white', padding: '8px 16px' }}>
  Iniciar Partida
</button>

// Incorrecto:
import { Button } from '@mui/material'; // NO
import { Button } from '@chakra-ui/react'; // NO
```

### Tipos TypeScript en `frontend/src/types/`
Todos los tipos compartidos van en archivos dedicados por dominio:
`mission.ts`, `session.ts`, `team.ts`, `game.ts`, `perfil.ts`, `usuario.ts`
