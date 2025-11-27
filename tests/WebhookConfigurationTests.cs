using Xunit;

namespace HomeAssistantWindowsVolumeSync.Tests;

/// <summary>
/// Tests for new webhook URL configuration structure with WebhookId, WebhookPath, and FullWebhookUrl
/// </summary>
public class WebhookConfigurationTests
{
    [Fact]
    public void FullWebhookUrl_BuildsCorrectly_WithAllComponents()
    {
        // Arrange
        var config = TestConfigurationHelper.CreateConfigurationWithWebhook(
            baseUrl: "https://test.local",
            webhookId: "my_webhook",
            webhookPath: "/api/webhook/"
        );

        // Act
        var fullUrl = config.FullWebhookUrl;

        // Assert
        Assert.Equal("https://test.local/api/webhook/my_webhook", fullUrl);
    }

    [Fact]
    public void FullWebhookUrl_HandlesTrailingSlashInBaseUrl()
    {
        // Arrange
        var config = TestConfigurationHelper.CreateConfigurationWithWebhook(
            baseUrl: "https://test.local/",
            webhookId: "my_webhook"
        );

        // Act
        var fullUrl = config.FullWebhookUrl;

        // Assert
        Assert.Equal("https://test.local/api/webhook/my_webhook", fullUrl);
    }

    [Fact]
    public void FullWebhookUrl_HandlesCustomWebhookPath()
    {
        // Arrange
        var config = TestConfigurationHelper.CreateConfigurationWithWebhook(
            baseUrl: "https://test.local",
            webhookId: "my_webhook",
            webhookPath: "/custom/path/"
        );

        // Act
        var fullUrl = config.FullWebhookUrl;

        // Assert
        Assert.Equal("https://test.local/custom/path/my_webhook", fullUrl);
    }

    [Fact]
    public void FullWebhookUrl_ReturnsNull_WhenWebhookUrlMissing()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "HomeAssistant:WebhookId", "test_webhook" },
            { "HomeAssistant:WebhookPath", "/api/webhook/" }
        };
        var config = TestConfigurationHelper.CreateConfiguration(configData);

        // Act
        var fullUrl = config.FullWebhookUrl;

        // Assert
        Assert.Null(fullUrl);
    }

    [Fact]
    public void FullWebhookUrl_ReturnsNull_WhenWebhookIdMissing()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "HomeAssistant:WebhookUrl", "https://test.local" },
            { "HomeAssistant:WebhookPath", "/api/webhook/" }
        };
        var config = TestConfigurationHelper.CreateConfiguration(configData);

        // Act
        var fullUrl = config.FullWebhookUrl;

        // Assert
        Assert.Null(fullUrl);
    }

    [Fact]
    public void WebhookPath_UsesDefaultValue_WhenNotConfigured()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "HomeAssistant:WebhookUrl", "https://test.local" },
            { "HomeAssistant:WebhookId", "test_webhook" }
        };
        var config = TestConfigurationHelper.CreateConfiguration(configData);

        // Act
        var path = config.WebhookPath;
        var fullUrl = config.FullWebhookUrl;

        // Assert
        Assert.Equal("/api/webhook/", path);
        Assert.Equal("https://test.local/api/webhook/test_webhook", fullUrl);
    }

    [Fact]
    public void WebhookId_ReturnsConfiguredValue()
    {
        // Arrange
        var config = TestConfigurationHelper.CreateConfigurationWithWebhook(
            webhookId: "my_custom_webhook_id"
        );

        // Act
        var webhookId = config.WebhookId;

        // Assert
        Assert.Equal("my_custom_webhook_id", webhookId);
    }

    [Fact]
    public void WebhookUrl_ReturnsBaseUrlOnly()
    {
        // Arrange
        var config = TestConfigurationHelper.CreateConfigurationWithWebhook(
            baseUrl: "https://my-home-assistant.local"
        );

        // Act
        var webhookUrl = config.WebhookUrl;

        // Assert
        Assert.Equal("https://my-home-assistant.local", webhookUrl);
    }

    [Theory]
    [InlineData("https://test.local", "test_id", "/api/webhook/", "https://test.local/api/webhook/test_id")]
    [InlineData("http://test.local", "webhook123", "/api/webhook/", "http://test.local/api/webhook/webhook123")]
    [InlineData("https://test.local/", "test", "/api/webhook", "https://test.local/api/webhook/test")]
    [InlineData("https://test.local", "test", "api/webhook/", "https://test.local/api/webhook/test")]
    public void FullWebhookUrl_BuildsCorrectly_WithVariousFormats(
        string baseUrl, string webhookId, string webhookPath, string expected)
    {
        // Arrange
        var config = TestConfigurationHelper.CreateConfigurationWithWebhook(
            baseUrl: baseUrl,
            webhookId: webhookId,
            webhookPath: webhookPath
        );

        // Act
        var fullUrl = config.FullWebhookUrl;

        // Assert
        Assert.Equal(expected, fullUrl);
    }
}
