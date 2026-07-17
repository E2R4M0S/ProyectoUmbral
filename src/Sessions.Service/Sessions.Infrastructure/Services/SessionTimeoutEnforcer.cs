using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sessions.Application.Sessions.Timeout;

namespace Sessions.Infrastructure.Services;

// Thin scheduler: periodically resolves a scoped SessionTimeoutEnforcementService and lets
// it do the actual enforcement (RF-02). Kept in Infrastructure because hosting/polling is an
// infrastructure concern; the business logic itself lives in the Application layer.
public class SessionTimeoutEnforcer : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionTimeoutEnforcer> _logger;

    public SessionTimeoutEnforcer(IServiceProvider serviceProvider, ILogger<SessionTimeoutEnforcer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var enforcementService = scope.ServiceProvider.GetRequiredService<SessionTimeoutEnforcementService>();
                    await enforcementService.EnforceActiveSessionsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Session timeout enforcement pass failed");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }
    }
}
