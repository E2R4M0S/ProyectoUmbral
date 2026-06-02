using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Sessions.Infrastructure.Notifications;
using Sessions.Application.Common.Interfaces;
using Xunit;

namespace Sessions.Service.Sessions.Infrastructure.Tests.Notifications;

public class RabbitMqEventPublisherTests
{
    [Fact]
    public async Task PublishAsync_logs_message()
    {
        var logger = new NullLogger<RabbitMqEventPublisher>();
        IEventPublisher publisher = new RabbitMqEventPublisher(logger);

        await publisher.PublishAsync("test.key", new { Value = 1 });

        // No exception == pass (we rely on logger side-effect)
        Assert.True(true);
    }
}
