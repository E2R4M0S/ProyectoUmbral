using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Sessions.Infrastructure.Notifications;
using Xunit;

namespace Sessions.Service.Sessions.Infrastructure.Tests.Notifications;

public class RabbitMqEventPublisherTests
{
    [Fact]
    public void Constructor_does_not_throw()
    {
        var logger = new NullLogger<RabbitMqEventPublisher>();
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

        var publisher = new RabbitMqEventPublisher(logger, config);

        Assert.NotNull(publisher);
    }
}
