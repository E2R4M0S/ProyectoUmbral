# Skill: WebSockets con SignalR — UMBRAL

## Arquitectura del RealTimeHub

El `RealTimeHub` es un microservicio independiente en `:5005` que:
1. Expone el `GameHub` de SignalR en `/hub/game`
2. Expone endpoints HTTP internos que otros servicios llaman para disparar broadcasts
3. **No tiene base de datos** — todo en memoria

```
Missions.Service  ─┐
Sessions.Service  ─┤─→ POST /internal/notifications/* ─→ RealTimeHub ─→ SignalR ─→ Clientes
Trivia.Service    ─┘
```

## GameHub (servidor)

```csharp
public class GameHub : Hub
{
    // Método que llaman los clientes para unirse al grupo de una sesión
    public async Task JoinSessionGroup(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"session:{sessionId}");
    }

    public async Task LeaveSessionGroup(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session:{sessionId}");
    }
}
```

## Endpoints HTTP internos (NotificationEndpoints)

```csharp
// Llamados por otros microservicios para disparar eventos SignalR
app.MapPost("/internal/notifications/session-status",
    async (SessionStatusNotification notification, IHubContext<GameHub> hub) =>
    {
        await hub.Clients.Group($"session:{notification.SessionId}")
            .SendAsync("SessionStatusChanged", new
            {
                notification.SessionId,
                notification.NewStatus
            });
        return Results.Ok();
    });

app.MapPost("/internal/notifications/clue-released",
    async (ClueReleasedNotification notification, IHubContext<GameHub> hub) =>
    {
        await hub.Clients.Group($"session:{notification.SessionId}")
            .SendAsync("ClueReleased", new
            {
                notification.StageId,
                notification.ClueContent
            });
        return Results.Ok();
    });

app.MapPost("/internal/notifications/question-asked",
    async (QuestionAskedNotification notification, IHubContext<GameHub> hub) =>
    {
        await hub.Clients.Group($"session:{notification.SessionId}")
            .SendAsync("QuestionAsked", new
            {
                notification.QuestionId,
                notification.Text,
                notification.Answers,
                notification.TimeLimitSeconds
            });
        return Results.Ok();
    });

app.MapPost("/internal/events/LeaderboardUpdated",
    async (List<LeaderboardEntryDto> leaderboard, IHubContext<GameHub> hub,
           HttpContext context) =>
    {
        var sessionId = context.Request.Query["sessionId"].ToString();
        await hub.Clients.Group($"session:{sessionId}")
            .SendAsync("RankingUpdated", leaderboard);
        return Results.Ok();
    });
```

## Cómo un servicio notifica al RealTimeHub

```csharp
// En Sessions.Service — IGameNotifier (interfaz en Application)
public interface IGameNotifier
{
    Task NotifyStatusChangedAsync(Guid sessionId, string newStatus, CancellationToken ct = default);
    Task NotifyProgressUpdatedAsync(Guid sessionId, object progress, CancellationToken ct = default);
    Task NotifyClueReleasedAsync(Guid sessionId, Guid stageId, string clueContent, CancellationToken ct = default);
}

// Implementación en Infrastructure — llama al RealTimeHub por HTTP
public class GameNotifier : IGameNotifier
{
    private readonly HttpClient _http;

    public GameNotifier(HttpClient http) => _http = http;

    public async Task NotifyStatusChangedAsync(Guid sessionId, string newStatus, CancellationToken ct = default)
    {
        await _http.PostAsJsonAsync("/internal/notifications/session-status", new
        {
            SessionId = sessionId,
            NewStatus = newStatus
        }, ct);
    }
}

// Registrar en DI
services.AddHttpClient<IGameNotifier, GameNotifier>(client =>
    client.BaseAddress = new Uri(configuration["RealTimeHub__Url"] ?? "http://realtimehub:80"));
```

## Frontend — useSignalR hook

```tsx
// frontend/src/hooks/useSignalR.ts
import { useEffect, useRef, useState } from 'react';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';

export function useSignalR(sessionId: string) {
  const [connection, setConnection] = useState<HubConnection | null>(null);
  const connectionRef = useRef<HubConnection | null>(null);

  useEffect(() => {
    const conn = new HubConnectionBuilder()
      .withUrl('/hub/game')  // el proxy de Vite redirige a :5005
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    conn.start()
      .then(() => conn.invoke('JoinSessionGroup', sessionId))
      .then(() => {
        connectionRef.current = conn;
        setConnection(conn);
      })
      .catch(console.error);

    return () => {
      conn.invoke('LeaveSessionGroup', sessionId).catch(() => {});
      conn.stop();
    };
  }, [sessionId]);

  return { connection };
}
```

## Frontend — suscribirse a eventos en un componente

```tsx
function PanelSesion({ sessionId }: { sessionId: string }) {
  const { connection } = useSignalR(sessionId);
  const [status, setStatus] = useState<string>('');
  const [clue, setClue] = useState<string | null>(null);
  const [leaderboard, setLeaderboard] = useState<LeaderboardEntry[]>([]);

  useEffect(() => {
    if (!connection) return;

    // Escuchar eventos
    connection.on('SessionStatusChanged', ({ newStatus }) => {
      setStatus(newStatus);
    });

    connection.on('ClueReleased', ({ clueContent }) => {
      setClue(clueContent);
    });

    connection.on('RankingUpdated', (entries: LeaderboardEntry[]) => {
      setLeaderboard(entries);
    });

    // Cleanup: desregistrar al desmontar
    return () => {
      connection.off('SessionStatusChanged');
      connection.off('ClueReleased');
      connection.off('RankingUpdated');
    };
  }, [connection]);

  // Render...
}
```

## Eventos disponibles y sus payloads

| Evento | Payload | Quién lo dispara |
|--------|---------|-----------------|
| `SessionStatusChanged` | `{ sessionId, newStatus }` | Sessions.Service al cambiar estado |
| `ProgressUpdated` | `{ sessionId, stages, participants }` | Sessions.Service al avanzar etapa |
| `ClueReleased` | `{ stageId, clueContent }` | Sessions.Service al liberar pista |
| `QuestionAsked` | `{ questionId, text, answers[], timeLimitSeconds }` | Trivia.Service al lanzar pregunta |
| `RankingUpdated` | `LeaderboardEntry[]` | Trivia.Service via RabbitMQ consumer |

## Configuración del proxy Vite (ya en el proyecto)

```typescript
// vite.config.ts
export default defineConfig({
  server: {
    proxy: {
      '/api': 'http://localhost:5000',
      '/hub': {
        target: 'http://localhost:5005',
        ws: true,  // WebSocket support
        changeOrigin: true
      }
    }
  }
});
```

## Gotchas importantes

1. **CORS en RealTimeHub es permisivo** (`AllowAnyOrigin + AllowCredentials`) — solo para dev. En producción configurar origins específicos.
2. **`withAutomaticReconnect()`** maneja reconexiones automáticamente — no implementes reconexión manual.
3. **Keycloak PKCE requiere HTTPS** — en dev, `disablePKCE: true` en `keycloak.ts`. Esto NO afecta a SignalR.
4. **Los grupos son efímeros** — si el RealTimeHub se reinicia, los clientes pierden el grupo. El frontend debe llamar `JoinSessionGroup` al reconectarse.
