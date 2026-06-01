using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Trivia.Application.Common.Interfaces;

namespace Trivia.Infrastructure.Messaging.RabbitMQ;

public class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqEventPublisher(IConfiguration configuration)
    {
        var factory = new ConnectionFactory()
        {
            HostName = configuration["RABBITMQ_HOST"] ?? "rabbitmq"
        };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare("trivia", ExchangeType.Topic, durable: true);
    }

    public Task PublishAsync(string eventName, object payload, CancellationToken ct = default)
    {
        var routingKey = eventName switch
        {
            "TriviaAnswerSubmittedEvent" => "answer.submitted",
            _ => eventName
        };

        var body = JsonSerializer.SerializeToUtf8Bytes(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var props = _channel.CreateBasicProperties();
        props.Persistent = true;
        _channel.BasicPublish(exchange: "trivia", routingKey: routingKey, basicProperties: props, body: body);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}
