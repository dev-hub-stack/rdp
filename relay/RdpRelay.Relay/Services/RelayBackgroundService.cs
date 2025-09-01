using Microsoft.Extensions.Options;
using RdpRelay.Relay.Models;

namespace RdpRelay.Relay.Services;

public class RelayBackgroundService : BackgroundService
{
    private readonly ILogger<RelayBackgroundService> _logger;
    private readonly ITcpRelayService _tcpRelayService;
    private readonly ISessionBroker _sessionBroker;
    private readonly IServiceProvider _serviceProvider;

    public RelayBackgroundService(
        ILogger<RelayBackgroundService> logger,
        ITcpRelayService tcpRelayService,
        ISessionBroker sessionBroker,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _tcpRelayService = tcpRelayService;
        _sessionBroker = sessionBroker;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Relay Background Service starting");

        // Start TCP relay service
        await _tcpRelayService.StartAsync(stoppingToken);

        // Start cleanup timer
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
                await _sessionBroker.CleanupExpiredSessionsAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background service cleanup");
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Relay Background Service stopping");
        await _tcpRelayService.StopAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}
