using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HomeAssistantWindowsVolumeSync;

/// <summary>
/// Service that periodically checks the connection to Home Assistant.
/// Marks connection as disconnected after consecutive failures (configurable via HealthCheckRetries).
/// </summary>
public class HealthCheckService : IHealthCheckService, IHostedService, IDisposable
{
    private readonly ILogger<HealthCheckService> _logger;
    private readonly IHomeAssistantClient _homeAssistantClient;
    private readonly IAppConfiguration _configuration;
    private readonly VolumeWatcherService _volumeWatcherService;
    private System.Threading.Timer? _healthCheckTimer;
    private int _consecutiveFailures;
    private bool _isConnected = true;
    private readonly object _stateLock = new object();

    public event EventHandler<bool>? ConnectionStateChanged;

    public bool IsConnected
    {
        get
        {
            lock (_stateLock)
            {
                return _isConnected;
            }
        }
    }

    public int ConsecutiveFailures
    {
        get
        {
            lock (_stateLock)
            {
                return _consecutiveFailures;
            }
        }
    }

    public HealthCheckService(
        ILogger<HealthCheckService> logger,
        IHomeAssistantClient homeAssistantClient,
        IAppConfiguration configuration,
        VolumeWatcherService volumeWatcherService)
    {
        _logger = logger;
        _homeAssistantClient = homeAssistantClient;
        _configuration = configuration;
        _volumeWatcherService = volumeWatcherService;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Health check service is starting...");

        // Perform initial health check
        _ = CheckHealthAsync();

        // Start periodic health checks
        var interval = _configuration.HealthCheckTimer;
        _healthCheckTimer = new System.Threading.Timer(
            _ => _ = Task.Run(async () => await CheckHealthAsync()),
            null,
            interval,
            interval);

        _logger.LogInformation("Health check service started. Checking every {Interval}ms", interval);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Health check service is stopping...");
        _healthCheckTimer?.Dispose();
        return Task.CompletedTask;
    }

    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            // Get current volume state to send with health check
            var volumeState = _volumeWatcherService.GetCurrentVolumeState();

            bool isHealthy;
            if (volumeState.HasValue)
            {
                // Send current volume as part of health check
                isHealthy = await _homeAssistantClient.CheckHealthAsync(
                    volumeState.Value.volumePercent,
                    volumeState.Value.isMuted);
            }
            else
            {
                // No volume data available, just check connectivity
                isHealthy = await _homeAssistantClient.CheckHealthAsync();
            }

            lock (_stateLock)
            {
                if (isHealthy)
                {
                    // Reset failure count on success
                    _consecutiveFailures = 0;

                    // Update connection state if it was previously disconnected
                    if (!_isConnected)
                    {
                        _isConnected = true;
                        _logger.LogInformation("Connection to Home Assistant restored");
                        OnConnectionStateChanged(true);
                    }
                }
                else
                {
                    // Increment failure count
                    _consecutiveFailures++;
                    _logger.LogWarning("Health check failed. Consecutive failures: {Count}", _consecutiveFailures);

                    // Mark as disconnected after threshold
                    if (_consecutiveFailures >= _configuration.HealthCheckRetries && _isConnected)
                    {
                        _isConnected = false;
                        _logger.LogError("Connection to Home Assistant lost after {Count} consecutive failures", _consecutiveFailures);
                        OnConnectionStateChanged(false);
                    }
                }
            }

            return isHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during health check");

            lock (_stateLock)
            {
                _consecutiveFailures++;

                if (_consecutiveFailures >= _configuration.HealthCheckRetries && _isConnected)
                {
                    _isConnected = false;
                    _logger.LogError("Connection to Home Assistant lost after {Count} consecutive failures", _consecutiveFailures);
                    OnConnectionStateChanged(false);
                }
            }

            return false;
        }
    }

    protected virtual void OnConnectionStateChanged(bool isConnected)
    {
        ConnectionStateChanged?.Invoke(this, isConnected);
    }

    public void Dispose()
    {
        _healthCheckTimer?.Dispose();
    }
}
