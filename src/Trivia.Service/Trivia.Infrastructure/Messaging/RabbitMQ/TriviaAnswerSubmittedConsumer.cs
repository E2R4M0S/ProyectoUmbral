using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace Trivia.Infrastructure.Messaging.RabbitMQ;

/// <summary>
/// Background service skeleton that would consume TriviaAnswerSubmittedEvent and update leaderboard.
/// Implementation is intentionally minimal: it deserializes the message and logs it.
/// A full implementation should update ParticipantAnswer table and recalculate leaderboard.
/// </summary>
public class TriviaAnswerSubmittedConsumer : BackgroundService
{
    private readonly ILogger<TriviaAnswerSubmittedConsumer> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public TriviaAnswerSubmittedConsumer(ILogger<TriviaAnswerSubmittedConsumer> logger)
    {
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var factory = new ConnectionFactory() { HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "rabbitmq" };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(queue: "trivia.answer.submitted", durable: true, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                _logger.LogInformation("Received TriviaAnswerSubmittedEvent: {msg}", message);
                // TODO: Deserialize and update leaderboard
                _channel.BasicAck(ea.DeliveryTag, false);
            };

            _channel.BasicConsume(queue: "trivia.answer.submitted", autoAck: false, consumer: consumer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start RabbitMQ consumer");
        }

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        base.Dispose();
    }
}
