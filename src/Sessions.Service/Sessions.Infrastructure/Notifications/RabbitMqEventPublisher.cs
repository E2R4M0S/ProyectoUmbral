using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using Sessions.Application.Common.Interfaces;

namespace Sessions.Infrastructure.Notifications
{
    public class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
    {
        private readonly ILogger<RabbitMqEventPublisher> _logger;
        private readonly string _exchange;
        private readonly ConnectionFactory _factory;
        private readonly Lazy<Task<(IConnection Connection, IChannel Channel)>> _connectionLazy;

        public RabbitMqEventPublisher(ILogger<RabbitMqEventPublisher> logger, IConfiguration configuration)
        {
            _logger = logger;

            _factory = new ConnectionFactory
            {
                HostName = configuration["RabbitMq:Host"] ?? "localhost",
                Port = int.TryParse(configuration["RabbitMq:Port"], out var p) ? p : 5672,
                UserName = configuration["RabbitMq:User"] ?? "guest",
                Password = configuration["RabbitMq:Password"] ?? "guest",
            };

            _exchange = configuration["RabbitMq:Exchange"] ?? "trivia.exchange";

            _connectionLazy = new Lazy<Task<(IConnection, IChannel)>>(InitializeAsync);
        }

        private async Task<(IConnection Connection, IChannel Channel)> InitializeAsync()
        {
            var connection = await _factory.CreateConnectionAsync();
            var channel = await connection.CreateChannelAsync();
            await channel.ExchangeDeclareAsync(_exchange, ExchangeType.Topic, durable: true);
            return (connection, channel);
        }

        public async Task PublishAsync(string routingKey, object payload, CancellationToken ct = default)
        {
            try
            {
                var (_, channel) = await _connectionLazy.Value;
                var json = JsonSerializer.Serialize(payload);
                var body = Encoding.UTF8.GetBytes(json);
                var props = new BasicProperties { ContentType = "application/json", DeliveryMode = DeliveryModes.Persistent };

                await channel.BasicPublishAsync(_exchange, routingKey, true, props, body);

                _logger.LogInformation("Published event {RoutingKey} to exchange {Exchange}", routingKey, _exchange);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish event {RoutingKey}", routingKey);
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                if (_connectionLazy.IsValueCreated)
                {
                    var (connection, channel) = await _connectionLazy.Value;
                    await channel.CloseAsync();
                    await connection.CloseAsync();
                }
            }
            catch { }
        }
    }
}
