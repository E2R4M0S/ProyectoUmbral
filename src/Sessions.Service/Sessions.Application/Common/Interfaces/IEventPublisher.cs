using System.Threading;
using System.Threading.Tasks;

namespace Sessions.Application.Common.Interfaces
{
    public interface IEventPublisher
    {
        Task PublishAsync(string routingKey, object payload, CancellationToken ct = default);
    }
}
