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
