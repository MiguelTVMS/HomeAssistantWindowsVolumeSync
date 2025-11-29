namespace HomeAssistantWindowsVolumeSync;

/// <summary>
/// Interface for centralized application configuration management.
/// Provides strongly-typed access to configuration values and change notifications.
/// </summary>
public interface IAppConfiguration
{
    /// <summary>
    /// Event raised when configuration is reloaded.
    /// </summary>
    event EventHandler? ConfigurationReloaded;

    /// <summary>
    /// Gets the Home Assistant base URL (without path or webhook ID).
    /// Example: https://your-home-assistant-url
    /// </summary>
    string? WebhookUrl { get; }

    /// <summary>
    /// Gets the webhook path (usually "/api/webhook/").
    /// </summary>
    string WebhookPath { get; }

    /// <summary>
    /// Gets the webhook ID used in the Home Assistant automation.
    /// Example: homeassistant_windows_volume_sync
    /// </summary>
    string? WebhookId { get; }

    /// <summary>
    /// Gets the complete webhook URL built from WebhookUrl + WebhookPath + WebhookId.
    /// Example: https://your-home-assistant-url/api/webhook/homeassistant_windows_volume_sync
    /// </summary>
    string? FullWebhookUrl { get; }

    /// <summary>
    /// Gets the target media player entity ID.
    /// </summary>
    string? TargetMediaPlayer { get; }

    /// <summary>
    /// Gets whether strict TLS validation is enabled.
    /// Defaults to true for security.
    /// </summary>
    bool StrictTLS { get; }

    /// <summary>
    /// Gets the time in milliseconds to wait after the last volume event before sending the webhook request.
    /// This debounces rapid volume changes to avoid flooding Home Assistant with requests.
    /// Defaults to 100ms.
    /// </summary>
    int DebounceTimer { get; }

    /// <summary>
    /// Gets the interval in milliseconds between health checks to Home Assistant.
    /// Health checks verify the connection is still active.
    /// Defaults to 5000ms (5 seconds).
    /// </summary>
    int HealthCheckTimer { get; }

    /// <summary>
    /// Gets the number of consecutive health check failures before marking the connection as disconnected.
    /// Defaults to 3.
    /// </summary>
    int HealthCheckRetries { get; }

    /// <summary>
    /// Reloads the configuration from the configuration source.
    /// This is called after settings are saved to apply changes immediately.
    /// </summary>
    void Reload();

    /// <summary>
    /// Gets a configuration value by key.
    /// </summary>
    /// <param name="key">The configuration key.</param>
    /// <returns>The configuration value or null if not found.</returns>
    string? GetValue(string key);

    /// <summary>
    /// Gets a configuration value by key with a default value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The configuration key.</param>
    /// <param name="defaultValue">The default value if key is not found.</param>
    /// <returns>The configuration value or the default value.</returns>
    T? GetValue<T>(string key, T defaultValue);
}
