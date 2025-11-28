namespace HomeAssistantWindowsVolumeSync;

/// <summary>
/// Interface for health check service that monitors connection to Home Assistant.
/// </summary>
public interface IHealthCheckService
{
    /// <summary>
    /// Event raised when the connection state changes.
    /// </summary>
    event EventHandler<bool>? ConnectionStateChanged;

    /// <summary>
    /// Gets whether the connection to Home Assistant is currently healthy.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Gets the number of consecutive failed health checks.
    /// </summary>
    int ConsecutiveFailures { get; }

    /// <summary>
    /// Starts the health check monitoring.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stops the health check monitoring.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Performs a single health check.
    /// </summary>
    Task<bool> CheckHealthAsync();
}
