using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Trivia.Application.Common.Interfaces;
using global::RabbitMQ.Client;
using System.Linq;

namespace Trivia.Infrastructure.Messaging.RabbitMQ;

public class RabbitMqEventPublisher : IEventPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqEventPublisher(IConfiguration configuration)
    {
        // Use the typed RabbitMQ client directly (package pinned in the infra project)
        var factory = new ConnectionFactory()
        {
            HostName = configuration["RABBITMQ_HOST"] ?? "rabbitmq",
            AutomaticRecoveryEnabled = true
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declare exchange (topic)
        _channel.ExchangeDeclare("trivia", ExchangeType.Topic, true);
    }

    public Task PublishAsync(string eventName, object payload, CancellationToken ct = default)
    {
        var routingKey = eventName switch
        {
            "TriviaAnswerSubmittedEvent" => "answer.submitted",
            _ => eventName
        };

        var body = JsonSerializer.SerializeToUtf8Bytes(payload, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        // Use reflection to create basic properties and publish
        var modelType = _channel.GetType();
        var createProps = modelType.GetMethod("CreateBasicProperties", Type.EmptyTypes);
        var props = createProps?.Invoke(_channel, null);
        if (props != null)
        {
            var persistentProp = props.GetType().GetProperty("Persistent");
            persistentProp?.SetValue(props, true);
        }

        var basicPublish = modelType.GetMethod("BasicPublish", new Type[] { typeof(string), typeof(string), props?.GetType() ?? typeof(object), typeof(byte[]) });
        if (basicPublish != null)
        {
            basicPublish.Invoke(_channel, new object[] { "trivia", routingKey, props, body });
        }
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        try { dynamic d = _channel; d?.Close(); } catch { }
        try { dynamic d2 = _connection; d2?.Close(); } catch { }
    }
}
