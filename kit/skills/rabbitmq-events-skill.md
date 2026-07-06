# Skill: RabbitMQ — Eventos de Dominio — UMBRAL

## Configuración actual

- **Exchange:** `trivia.exchange` (type: `topic`)
- **Cola activa:** `trivia.answer.submitted`
- **Routing key publicada por Sessions.Service:** `session.status.changed`
- **Routing key consumida por Trivia.Service:** `answer.submitted`
- **Consumer:** `TriviaAnswerSubmittedConsumer` (BackgroundService, polling cada 500ms con `BasicGet`)

## Patrón Publisher (Sessions.Service / Trivia.Service)

```csharp
// Domain Interface
public interface IEventPublisher
{
    Task PublishAsync<T>(string routingKey, T payload, CancellationToken ct = default);
}

// Infrastructure Implementation
public class RabbitMqEventPublisher : IEventPublisher
{
    private readonly IConfiguration _config;

    public RabbitMqEventPublisher(IConfiguration config) => _config = config;

    public async Task PublishAsync<T>(string routingKey, T payload, CancellationToken ct = default)
    {
        var factory = new ConnectionFactory
        {
            HostName = _config["RabbitMq__Host"] ?? "rabbitmq"
        };

        using var connection = await factory.CreateConnectionAsync(ct);
        using var channel = await connection.CreateChannelAsync();

        var exchange = _config["RabbitMq__Exchange"] ?? "trivia.exchange";
        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        var props = new BasicProperties { Persistent = true };

        await channel.BasicPublishAsync(
            exchange: exchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct
        );
    }
}
```

## Cómo publicar desde un Handler

```csharp
public class TransitionSessionCommandHandler : IRequestHandler<TransitionSessionCommand>
{
    private readonly ISessionRepository _repo;
    private readonly IEventPublisher _publisher;
    private readonly IGameNotifier _notifier;

    public TransitionSessionCommandHandler(
        ISessionRepository repo,
        IEventPublisher publisher,
        IGameNotifier notifier)
    {
        _repo = repo;
        _publisher = publisher;
        _notifier = notifier;
    }

    public async Task Handle(TransitionSessionCommand command, CancellationToken ct)
    {
        var session = await _repo.GetByIdAsync(command.SessionId, ct)
            ?? throw new NotFoundException($"Session {command.SessionId} not found");

        // Transición en la entidad de dominio
        session.TransitionTo(command.NewStatus);
        await _repo.UpdateAsync(session, ct);

        // Publicar evento de dominio
        await _publisher.PublishAsync("session.status.changed", new
        {
            SessionId = session.Id,
            NewStatus = session.Status.ToString(),
            OccurredAt = DateTime.UtcNow
        }, ct);

        // Notificar via SignalR
        await _notifier.NotifyStatusChangedAsync(session.Id, session.Status.ToString(), ct);
    }
}
```

## Patrón Consumer (BackgroundService)

```csharp
public class TriviaAnswerSubmittedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;

    public TriviaAnswerSubmittedConsumer(IServiceScopeFactory scopeFactory, IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _config["RabbitMq__Host"] ?? "rabbitmq"
        };

        using var connection = await factory.CreateConnectionAsync(stoppingToken);
        using var channel = await connection.CreateChannelAsync();

        await channel.QueueDeclareAsync(
            queue: "trivia.answer.submitted",
            durable: true,
            exclusive: false,
            autoDelete: false
        );

        await channel.QueueBindAsync(
            queue: "trivia.answer.submitted",
            exchange: "trivia",
            routingKey: "answer.submitted"
        );

        while (!stoppingToken.IsCancellationRequested)
        {
            var result = await channel.BasicGetAsync("trivia.answer.submitted", autoAck: false, stoppingToken);

            if (result is not null)
            {
                try
                {
                    var json = Encoding.UTF8.GetString(result.Body.ToArray());
                    var payload = JsonSerializer.Deserialize<TriviaAnswerSubmittedEvent>(json)!;

                    using var scope = _scopeFactory.CreateScope();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                    await mediator.Send(new UpdateLeaderboardCommand(
                        payload.QuizId,
                        payload.TeamId,
                        payload.TeamName,
                        payload.IsCorrect,
                        payload.PointsAwarded
                    ), stoppingToken);

                    await channel.BasicAckAsync(result.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    // Nack sin requeue para evitar poison messages
                    await channel.BasicNackAsync(result.DeliveryTag, multiple: false, requeue: false);
                }
            }
            else
            {
                // No hay mensajes — esperar antes de reintentar
                await Task.Delay(500, stoppingToken);
            }
        }
    }
}
```

## Registro del Consumer

```csharp
// En Infrastructure/DependencyInjection.cs
services.AddHostedService<TriviaAnswerSubmittedConsumer>();
services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
```

## Agregar un nuevo evento

1. Definir el evento como record en `Domain/Events/`:
   ```csharp
   public record SessionStatusChangedEvent(Guid SessionId, string NewStatus, DateTime OccurredAt);
   ```

2. Publicar desde el Handler con la routing key correspondiente:
   ```csharp
   await _publisher.PublishAsync("session.status.changed", new SessionStatusChangedEvent(...), ct);
   ```

3. Si otro servicio necesita consumirlo, crear un nuevo `BackgroundService` consumer con `QueueBind` a la routing key.

## Variables de entorno necesarias

```yaml
# docker-compose.yml
environment:
  - RabbitMq__Host=rabbitmq
  - RabbitMq__Exchange=trivia.exchange
```

```bash
# Para desarrollo local (sin Docker)
RabbitMq__Host=localhost
```
