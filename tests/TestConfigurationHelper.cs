using Microsoft.Extensions.Configuration;

namespace HomeAssistantWindowsVolumeSync.Tests;

/// <summary>
/// Helper class for creating test configurations
/// </summary>
public static class TestConfigurationHelper
{
    public static IAppConfiguration CreateConfiguration(Dictionary<string, string?> configData)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
        return new AppConfiguration(config);
    }

    /// <summary>
    /// Creates a test configuration with webhook URL split into components
    /// </summary>
    public static IAppConfiguration CreateConfigurationWithWebhook(
        string baseUrl = "https://test-ha.local",
        string webhookId = "test_webhook",
        string webhookPath = "/api/webhook/",
        string? targetMediaPlayer = null,
        bool strictTls = true,
        string audioDeviceId = "")
    {
        var configData = new Dictionary<string, string?>
        {
            { "HomeAssistant:WebhookUrl", baseUrl },
            { "HomeAssistant:WebhookPath", webhookPath },
            { "HomeAssistant:WebhookId", webhookId },
            { "HomeAssistant:TargetMediaPlayer", targetMediaPlayer ?? "media_player.test" },
            { "HomeAssistant:StrictTLS", strictTls.ToString() },
            { "HomeAssistant:AudioDeviceId", audioDeviceId }
        };

        return CreateConfiguration(configData);
    }
}
