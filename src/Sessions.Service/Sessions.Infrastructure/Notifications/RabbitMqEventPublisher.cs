using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sessions.Application.Common.Interfaces;

namespace Sessions.Infrastructure.Notifications
{
    // Lightweight placeholder implementation that would normally use RabbitMQ client
    public class RabbitMqEventPublisher : IEventPublisher
    {
        private readonly ILogger<RabbitMqEventPublisher> _logger;

        public RabbitMqEventPublisher(ILogger<RabbitMqEventPublisher> logger)
        {
            _logger = logger;
        }

        public Task PublishAsync(string routingKey, object payload)
        {
            // For test/dev environment we simply log the publishing event
            var json = JsonSerializer.Serialize(payload);
            _logger.LogInformation("[RabbitMqEventPublisher] publish {RoutingKey} {Payload}", routingKey, json);
            return Task.CompletedTask;
        }
    }
}
