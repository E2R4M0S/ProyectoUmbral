# Skill: Testing — UMBRAL

## Stack de testing del proyecto

**Backend:** xUnit + FluentAssertions + NSubstitute
**Frontend:** Vitest + @testing-library/react

**IMPORTANTE:** El proyecto usa **NSubstitute**, no Moq. Si ves `.Setup()` o `.Verify()` es código Moq — no lo uses aquí.

## NSubstitute — Referencia rápida

```csharp
// Crear mock
var repo = Substitute.For<IMissionRepository>();

// Configurar retorno
repo.GetByIdAsync(missionId, Arg.Any<CancellationToken>())
    .Returns(Task.FromResult<Mission?>(existingMission));

// Verificar que se llamó
await repo.Received(1).AddAsync(
    Arg.Is<Mission>(m => m.Title == "Mission Alpha"),
    Arg.Any<CancellationToken>()
);

// Verificar que NO se llamó
await repo.DidNotReceive().UpdateAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>());

// Configurar lanzar excepción
repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
    .Returns(Task.FromException<Mission?>(new Exception("DB error")));

// Capturar argumento
Mission? capturedMission = null;
await repo.AddAsync(
    Arg.Do<Mission>(m => capturedMission = m),
    Arg.Any<CancellationToken>()
);
```

## Template: Test de Command Handler

```csharp
public class CreateMissionCommandHandlerTests
{
    private readonly IMissionRepository _repo;
    private readonly CreateMissionCommandHandler _handler;

    public CreateMissionCommandHandlerTests()
    {
        _repo = Substitute.For<IMissionRepository>();
        _handler = new CreateMissionCommandHandler(_repo);
    }

    [Fact]
    public async Task Given_ValidCommand_When_Handled_Then_MissionIsCreatedAndAdded()
    {
        // Arrange
        _repo.ExistsByTitleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
             .Returns(false);

        var command = new CreateMissionCommand(
            "Operation Thunderstrike",
            "A challenging mission",
            Difficulty.Hard,
            60,
            MissionType.Treasure
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.MissionId.Should().NotBeEmpty();

        await _repo.Received(1).AddAsync(
            Arg.Is<Mission>(m =>
                m.Title == "Operation Thunderstrike" &&
                m.Status == MissionStatus.Draft &&
                m.Difficulty == Difficulty.Hard
            ),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task Given_DuplicateTitle_When_Handled_Then_ExceptionIsThrown()
    {
        // Arrange
        _repo.ExistsByTitleAsync("Existing Mission", Arg.Any<CancellationToken>())
             .Returns(true);

        var command = new CreateMissionCommand("Existing Mission", "desc", Difficulty.Easy, 30, MissionType.Trivia);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");

        await _repo.DidNotReceive().AddAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>());
    }
}
```

## Template: Test de Entidad de Dominio

```csharp
public class SessionDomainTests
{
    [Fact]
    public void Given_NewSession_Then_StatusIsScheduled()
    {
        var session = Session.Create("Test Session", new[] { Guid.NewGuid() });
        session.Status.Should().Be(SessionStatus.Scheduled);
    }

    [Fact]
    public void Given_ScheduledSession_When_Prepare_Then_StatusIsPreparing()
    {
        var session = Session.Create("Test Session", new[] { Guid.NewGuid() });
        session.Prepare();
        session.Status.Should().Be(SessionStatus.Preparing);
    }

    [Fact]
    public void Given_FinishedSession_When_Prepare_Then_Throws()
    {
        var session = Session.Create("Test Session", new[] { Guid.NewGuid() });
        session.Prepare();
        session.Start();
        session.Finish();

        var act = () => session.Prepare();

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData(SessionStatus.Finished)]
    [InlineData(SessionStatus.Cancelled)]
    public void Given_TerminalStatus_When_Cancel_Then_Throws(SessionStatus terminalStatus)
    {
        // Arrange — crear sesión en el estado terminal
        var session = CreateSessionInStatus(terminalStatus);

        // Act & Assert
        var act = () => session.Cancel();
        act.Should().Throw<InvalidOperationException>();
    }
}
```

## Template: Test de Validator (FluentValidation)

```csharp
public class CreateMissionCommandValidatorTests
{
    private readonly CreateMissionCommandValidator _validator = new();

    [Fact]
    public async Task Given_EmptyTitle_Then_ValidationFails()
    {
        var command = new CreateMissionCommand("", "desc", Difficulty.Easy, 30, MissionType.Treasure);
        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Title));
    }

    [Fact]
    public async Task Given_TitleOver200Chars_Then_ValidationFails()
    {
        var command = new CreateMissionCommand(new string('A', 201), "desc", Difficulty.Easy, 30, MissionType.Treasure);
        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Given_ValidCommand_Then_ValidationPasses()
    {
        var command = new CreateMissionCommand("Valid Title", "desc", Difficulty.Medium, 60, MissionType.Treasure);
        var result = await _validator.ValidateAsync(command);

        result.IsValid.Should().BeTrue();
    }
}
```

## Template: Test frontend (Vitest + Testing Library)

```tsx
// ClueCard.test.tsx
import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { ClueCard } from './ClueCard';

describe('ClueCard', () => {
  it('renders clue content', () => {
    render(<ClueCard content="The answer is hidden under the red table" penalty={10} />);
    expect(screen.getByText('The answer is hidden under the red table')).toBeInTheDocument();
  });

  it('shows penalty when provided', () => {
    render(<ClueCard content="Clue" penalty={15} />);
    expect(screen.getByText(/-15/)).toBeInTheDocument();
  });

  it('does not show penalty when zero', () => {
    render(<ClueCard content="Free clue" penalty={0} />);
    expect(screen.queryByText(/-0/)).not.toBeInTheDocument();
  });
});
```

## Correr tests

```bash
# Backend — por solución
dotnet test src/Sessions.Service/Sessions.Service.slnx --verbosity normal

# Con reporte de coverage
dotnet test src/Sessions.Service/Sessions.Service.slnx \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults

# Solo un test específico
dotnet test --filter "FullyQualifiedName~SessionDomainTests"

# Frontend
cd frontend && npm test
cd frontend && npm test -- --watch  # modo watch
```

## Anti-patrones a evitar

```csharp
// MAL: test que siempre pasa
[Fact]
public async Task Test_Handler()
{
    var result = await _handler.Handle(command, default);
    result.Should().NotBeNull(); // esto nunca falla
}

// MAL: sin verificar side effects
[Fact]
public async Task Given_ValidCommand_Then_Succeeds()
{
    var result = await _handler.Handle(command, default);
    // No verificamos que se guardó en el repositorio!
}

// BIEN: verifica comportamiento real
[Fact]
public async Task Given_ValidCommand_Then_MissionIsPersisted()
{
    var result = await _handler.Handle(command, default);
    result.MissionId.Should().NotBeEmpty();
    await _repo.Received(1).AddAsync(Arg.Any<Mission>(), Arg.Any<CancellationToken>());
}
```
