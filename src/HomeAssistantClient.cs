using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace HomeAssistantWindowsVolumeSync;

/// <summary>
/// Client for sending volume updates to Home Assistant via webhook.
/// </summary>
public class HomeAssistantClient : IHomeAssistantClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HomeAssistantClient> _logger;
    private readonly IAppConfiguration _configuration;
    private string? _webhookUrl;
    private string? _targetMediaPlayer;
    private bool _strictTls;

    public HomeAssistantClient(
        HttpClient httpClient,
        ILogger<HomeAssistantClient> logger,
        IAppConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;

        // Subscribe to configuration changes
        _configuration.ConfigurationReloaded += OnConfigurationReloaded;

        LoadConfiguration();
    }

    private void LoadConfiguration()
    {
        _webhookUrl = _configuration.FullWebhookUrl;
        _targetMediaPlayer = _configuration.TargetMediaPlayer;
        _strictTls = _configuration.StrictTLS;

        if (string.IsNullOrEmpty(_webhookUrl))
        {
            _logger.LogWarning("HomeAssistant webhook URL is not configured. Please set 'HomeAssistant:WebhookUrl' and 'HomeAssistant:WebhookId' in appsettings.json");
        }
        else
        {
            var uri = new Uri(_webhookUrl);
            if (uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Using HTTP (unencrypted) connection to Home Assistant: {Url}", _webhookUrl);
            }
            else if (uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                if (!_strictTls)
                {
                    _logger.LogWarning("Using HTTPS with certificate validation disabled (StrictTLS=false)");
                }
            }
        }

        if (string.IsNullOrEmpty(_targetMediaPlayer))
        {
            _logger.LogWarning("Target media player is not configured. Please set 'HomeAssistant:TargetMediaPlayer' in appsettings.json");
        }
    }

    private void OnConfigurationReloaded(object? sender, EventArgs e)
    {
        _logger.LogInformation("Configuration reloaded, updating HomeAssistantClient settings");
        LoadConfiguration();
    }

    /// <inheritdoc/>
    public async Task SendVolumeUpdateAsync(int volumePercent, bool isMuted)
    {
        if (string.IsNullOrEmpty(_webhookUrl))
        {
            _logger.LogWarning("Cannot send volume update: Webhook URL is not configured");
            return;
        }

        try
        {
            var payload = new VolumePayload
            {
                Volume = volumePercent,
                Mute = isMuted,
                TargetMediaPlayer = _targetMediaPlayer
            };

            var response = await _httpClient.PostAsJsonAsync(_webhookUrl, payload);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Home Assistant webhook returned status code {StatusCode}", response.StatusCode);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to send volume update to Home Assistant");
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Request to Home Assistant timed out");
        }
    }

    /// <inheritdoc/>
    public async Task<bool> CheckHealthAsync()
    {
        if (string.IsNullOrEmpty(_webhookUrl))
        {
            _logger.LogDebug("Cannot check health: Webhook URL is not configured");
            return false;
        }

        try
        {
            // Send a minimal request to check if Home Assistant is reachable
            // We use a HEAD request or a simple GET to avoid triggering automations
            var response = await _httpClient.GetAsync(_configuration.WebhookUrl, HttpCompletionOption.ResponseHeadersRead);

            // Only 2XX status codes indicate a healthy connection
            // 4XX/5XX errors mean something is wrong with the server or configuration
            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Health check successful: Status {StatusCode}", response.StatusCode);
                return true;
            }
            else
            {
                _logger.LogWarning("Health check failed: Status {StatusCode}", response.StatusCode);
                return false;
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogDebug(ex, "Health check failed: HTTP request error");
            return false;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogDebug(ex, "Health check failed: Request timed out");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Health check failed: Unexpected error");
            return false;
        }
    }

    /// <summary>
    /// Payload sent to the Home Assistant webhook.
    /// </summary>
    private class VolumePayload
    {
        public int Volume { get; set; }
        public bool Mute { get; set; }
        public string? TargetMediaPlayer { get; set; }
    }
}
