using Microsoft.Extensions.Configuration;

namespace HomeAssistantWindowsVolumeSync;

/// <summary>
/// Centralized application configuration manager with hot-reload support.
/// Wraps IConfiguration and provides strongly-typed access to settings.
/// </summary>
public class AppConfiguration : IAppConfiguration
{
    private readonly IConfiguration _configuration;
    private readonly IConfigurationRoot? _configurationRoot;

    /// <inheritdoc/>
    public event EventHandler? ConfigurationReloaded;

    public AppConfiguration(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _configurationRoot = configuration as IConfigurationRoot;
    }

    /// <inheritdoc/>
    public string? WebhookUrl => _configuration["HomeAssistant:WebhookUrl"];

    /// <inheritdoc/>
    public string WebhookPath => _configuration["HomeAssistant:WebhookPath"] ?? "/api/webhook/";

    /// <inheritdoc/>
    public string? WebhookId => _configuration["HomeAssistant:WebhookId"];

    /// <inheritdoc/>
    public string? FullWebhookUrl
    {
        get
        {
            if (string.IsNullOrEmpty(WebhookUrl) || string.IsNullOrEmpty(WebhookId))
            {
                return null;
            }

            // Remove trailing slash from URL if present
            var baseUrl = WebhookUrl.TrimEnd('/');

            // Ensure path starts with / and remove trailing slash
            var path = WebhookPath;
            if (!path.StartsWith('/'))
            {
                path = "/" + path;
            }
            path = path.TrimEnd('/');

            return $"{baseUrl}{path}/{WebhookId}";
        }
    }

    /// <inheritdoc/>
    public string? TargetMediaPlayer => _configuration["HomeAssistant:TargetMediaPlayer"];

    /// <inheritdoc/>
    public bool StrictTLS => _configuration.GetValue<bool>("HomeAssistant:StrictTLS", true);

    /// <inheritdoc/>
    public void Reload()
    {
        if (_configurationRoot != null)
        {
            _configurationRoot.Reload();
            OnConfigurationReloaded();
        }
    }

    /// <inheritdoc/>
    public string? GetValue(string key)
    {
        return _configuration[key];
    }

    /// <inheritdoc/>
    public T GetValue<T>(string key, T defaultValue)
    {
        return _configuration.GetValue<T>(key, defaultValue);
    }

    /// <summary>
    /// Raises the ConfigurationReloaded event.
    /// </summary>
    protected virtual void OnConfigurationReloaded()
    {
        ConfigurationReloaded?.Invoke(this, EventArgs.Empty);
    }
}
