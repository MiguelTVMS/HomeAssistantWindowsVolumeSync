using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HomeAssistantWindowsVolumeSync;

/// <summary>
/// Client for sending volume updates to Home Assistant via webhook.
/// </summary>
public class HomeAssistantClient : IHomeAssistantClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HomeAssistantClient> _logger;
    private readonly string? _webhookUrl;

    public HomeAssistantClient(
        HttpClient httpClient,
        ILogger<HomeAssistantClient> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _webhookUrl = configuration["HomeAssistant:WebhookUrl"];

        if (string.IsNullOrEmpty(_webhookUrl))
        {
            _logger.LogWarning("HomeAssistant webhook URL is not configured. Please set 'HomeAssistant:WebhookUrl' in appsettings.json");
        }
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
                Mute = isMuted
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
    }
}
