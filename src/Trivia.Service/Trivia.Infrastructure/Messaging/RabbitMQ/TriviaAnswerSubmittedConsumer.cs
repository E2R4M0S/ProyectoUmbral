using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace Trivia.Infrastructure.Messaging.RabbitMQ;

/// <summary>
/// Background service skeleton that would consume TriviaAnswerSubmittedEvent and update leaderboard.
/// Implementation is intentionally minimal: it deserializes the message and logs it.
/// A full implementation should update ParticipantAnswer table and recalculate leaderboard.
/// </summary>
public class TriviaAnswerSubmittedConsumer : BackgroundService
{
    private readonly ILogger<TriviaAnswerSubmittedConsumer> _logger;
    private object? _connection;
    private object? _channel;

    public TriviaAnswerSubmittedConsumer(ILogger<TriviaAnswerSubmittedConsumer> logger)
    {
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            dynamic factory = new global::RabbitMQ.Client.ConnectionFactory() { HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "rabbitmq" };
            var conn = factory.CreateConnection();
            var ch = conn.CreateModel();
            _connection = conn;
            _channel = ch;

            dynamic _ch = _channel;
            _ch.ExchangeDeclare("trivia", global::RabbitMQ.Client.ExchangeType.Topic, durable: true);
            _ch.QueueDeclare(queue: "trivia.answer.submitted", durable: true, exclusive: false, autoDelete: false, arguments: null);
            _ch.QueueBind("trivia.answer.submitted", "trivia", "answer.submitted");

            var consumer = new global::RabbitMQ.Client.Events.EventingBasicConsumer(_ch);
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                _logger.LogInformation("Received TriviaAnswerSubmittedEvent: {msg}", message);
                // TODO: Deserialize and update leaderboard
                _ch.BasicAck(ea.DeliveryTag, false);
            };

            _ch.BasicConsume(queue: "trivia.answer.submitted", autoAck: false, consumer: consumer);
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
