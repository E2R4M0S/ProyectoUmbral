using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Microsoft.Extensions.Configuration;
using Trivia.Application.Common.Interfaces;

namespace Trivia.Infrastructure.Messaging.RabbitMQ;

public class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private readonly object _connection;
    private readonly object _channel;

    public RabbitMqEventPublisher(IConfiguration configuration)
    {
        dynamic factory = new global::RabbitMQ.Client.ConnectionFactory()
        {
            HostName = configuration["RABBITMQ_HOST"] ?? "rabbitmq"
        };
        var conn = factory.CreateConnection();
        var ch = conn.CreateModel();
        _connection = conn;
        _channel = ch;
        // Use dynamic to avoid compile-time dependency on IModel signatures in certain SDKs
        dynamic _ch = _channel;
        _ch.ExchangeDeclare("trivia", global::RabbitMQ.Client.ExchangeType.Topic, durable: true);
    }

    public Task PublishAsync(string eventName, object payload, CancellationToken ct = default)
    {
        var routingKey = eventName switch
        {
            "TriviaAnswerSubmittedEvent" => "answer.submitted",
            _ => eventName
        };

        var body = JsonSerializer.SerializeToUtf8Bytes(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        dynamic _ch = _channel;
        var props = _ch.CreateBasicProperties();
        props.Persistent = true;
        _ch.BasicPublish(exchange: "trivia", routingKey: routingKey, basicProperties: props, body: body);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        try
        {
            dynamic _ch = _channel;
            _ch?.Close();
        }
        catch { }
        try
        {
            dynamic _conn = _connection;
            _conn?.Close();
        }
        catch { }
    }
}
