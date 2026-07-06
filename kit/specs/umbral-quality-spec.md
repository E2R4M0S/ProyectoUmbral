# Quality Spec — UMBRAL v2

## Objetivos de Calidad

| Criterio | Objetivo |
|----------|---------|
| Cobertura tests backend | >= 90% |
| Tests pasando actualmente | 248 (julio 2026) |
| Tests E2E | Pendiente (Playwright) |
| Pipeline CI/CD | Pendiente (GitHub Actions) |

---

## Estrategia de Testing por Capa

### 1. Domain Tests (más importantes)
Testean el núcleo del negocio sin infraestructura. Son los más rápidos y más valiosos.

**Qué testear:**
- Factory methods: creación válida, argumentos inválidos, estado inicial
- Métodos de comportamiento: transiciones de estado, validaciones de negocio
- Value Objects: igualdad, validaciones, factory methods

```csharp
// Ejemplo: Sessions.Domain.Tests
public class SessionStateTransitionTests
{
    [Fact]
    public void Given_ScheduledSession_When_Prepare_Then_StatusIsPreparing()

    [Theory]
    [InlineData(SessionStatus.Scheduled)]
    [InlineData(SessionStatus.Preparing)]
    [InlineData(SessionStatus.Active)]
    [InlineData(SessionStatus.Paused)]
    public void Given_NonTerminalSession_When_Cancel_Then_Succeeds(SessionStatus initial)

    [Theory]
    [InlineData(SessionStatus.Finished)]
    [InlineData(SessionStatus.Cancelled)]
    public void Given_TerminalSession_When_Cancel_Then_Throws(SessionStatus terminal)
}
```

### 2. Application Tests (handlers, validators)
Testean la orquestación de casos de uso. Usan mocks para repositorios y servicios externos.

**Stack:** xUnit + FluentAssertions + NSubstitute

**Qué testear:**
- Happy path: comando/query procesado correctamente, side effects verificados
- Casos de error: entidad no encontrada, datos inválidos, regla de negocio violada
- Validators: cada regla de FluentValidation por separado

```csharp
// Verificar side effects con NSubstitute
await _repo.Received(1).AddAsync(
    Arg.Is<Mission>(m => m.Title == "Test" && m.Status == MissionStatus.Draft),
    Arg.Any<CancellationToken>()
);

// Verificar que NO ocurre algo
await _repo.DidNotReceive().UpdateAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>());
```

### 3. Infrastructure Tests (repositorios)
Tests de integración contra base de datos real o en memoria.

**Opción A — EF Core InMemory** (más rápido, para pruebas simples)
```csharp
var options = new DbContextOptionsBuilder<MissionsDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString())
    .Options;
var db = new MissionsDbContext(options);
var repo = new MissionRepository(db);
```

**Opción B — PostgreSQL real en Docker** (más fiel a producción)
```csharp
// Usar variable de entorno para la connection string en CI
var connectionString = Environment.GetEnvironmentVariable("TEST_DB") 
    ?? "Host=localhost;Port=5432;Database=test_missions;...";
```

### 4. API Tests (endpoints)
Tests de integración que verifican status codes y contratos de la API.

```csharp
public class MissionsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    
    public MissionsApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task POST_missions_Returns201_WithValidBody()
    {
        var response = await _client.PostAsJsonAsync("/missions", new
        {
            title = "Test Mission",
            description = "desc",
            difficulty = "Easy",
            timeMinutes = 30,
            type = "Treasure"
        });
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
```

### 5. Frontend Tests (Vitest + Testing Library)

```tsx
// Testear comportamiento, no implementación
it('shows clue content when rendered', () => {
    render(<ClueCard content="Hidden under the red table" penalty={10} />);
    expect(screen.getByText('Hidden under the red table')).toBeInTheDocument();
});

// Testear interacción
it('calls onAnswer when option is clicked', async () => {
    const onAnswer = vi.fn();
    render(<QuestionCard question={mockQuestion} onAnswer={onAnswer} />);
    await userEvent.click(screen.getByText('Option A'));
    expect(onAnswer).toHaveBeenCalledWith('answer-id-a');
});
```

---

## Convenciones de Naming (resumen)

```
Clase de tests:    Given_<contexto>
Método de test:    Given_<contexto>_When_<acción>_Then_<resultado>

Ejemplos válidos:
  Given_CreateMissionCommand_When_TitleIsEmpty_Then_ValidationFails
  Given_ScheduledSession_When_StartWithoutParticipants_Then_ThrowsException
  Given_ValidJoinCode_When_ParticipantJoins_Then_MemberIsAdded

Ejemplos inválidos (no usar):
  Test1
  Should_Work
  CreateMission_Success
```

---

## Proyectos de tests por servicio

```
src/
├── ApiGateway.Tests/
├── Missions.Service/
│   └── Missions.Domain.Tests/
├── Sessions.Service/
│   └── Sessions.Domain.Tests/
├── Teams.Service/
│   └── Teams.Domain.Tests/
├── Trivia.Service/
│   └── Trivia.Domain.Tests/
└── RealTimeHub.Tests/
```

Cada servicio puede tener tests por capa:
```
Missions.Domain.Tests/         ← entidades, VOs
Missions.Application.Tests/   ← handlers, validators
Missions.Infrastructure.Tests/ ← repos con EF InMemory
Missions.Api.Tests/           ← endpoints con WebApplicationFactory
```

---

## Comandos de testing

```bash
# Correr todos los tests de un servicio
dotnet test src/Sessions.Service/Sessions.Service.slnx --verbosity normal

# Con coverage
dotnet test src/Sessions.Service/Sessions.Service.slnx \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults

# Generar reporte HTML (requiere ReportGenerator)
reportgenerator -reports:./TestResults/**/*.xml -targetdir:./TestResults/html -reporttypes:Html

# Solo tests que matcheen un patrón
dotnet test --filter "FullyQualifiedName~SessionDomain"
dotnet test --filter "Category=Unit"

# Frontend
cd frontend
npm test                    # run once
npm test -- --watch         # modo watch
npm test -- --coverage      # con coverage
```

---

## CI/CD (a implementar)

### `.github/workflows/ci.yml`

```yaml
name: CI — UMBRAL

on:
  push:
    branches: [develop, main]
  pull_request:
    branches: [develop]

jobs:
  backend-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      - name: Run Missions tests
        run: dotnet test src/Missions.Service/Missions.Service.slnx --collect:"XPlat Code Coverage"
      
      - name: Run Sessions tests
        run: dotnet test src/Sessions.Service/Sessions.Service.slnx --collect:"XPlat Code Coverage"
      
      - name: Run Teams tests
        run: dotnet test src/Teams.Service/Teams.Service.slnx --collect:"XPlat Code Coverage"
      
      - name: Run Trivia tests
        run: dotnet test src/Trivia.Service/Trivia.Service.slnx --collect:"XPlat Code Coverage"

  frontend-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: '20'
      - run: cd frontend && npm ci && npm test
```

---

## Deuda técnica conocida

| Área | Deuda | Prioridad |
|------|-------|-----------|
| Tests E2E | `tests/` vacío — no hay Playwright configurado | Media |
| CI/CD | No hay `.github/workflows/` | Alta |
| Coverage report | No está configurado en el pipeline | Media |
| RealTimeHub CORS | `AllowAnyOrigin` es inseguro para producción | Baja (solo dev) |
| SSL/TLS | `RequireHttpsMetadata = false` en todos los servicios | Baja (solo dev) |
| EF Migrations | `EnsureCreated()` no permite evolucionar el schema | Media |
