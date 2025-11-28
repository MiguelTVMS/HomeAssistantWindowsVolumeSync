using Microsoft.Extensions.Configuration;
using Xunit;

namespace HomeAssistantWindowsVolumeSync.Tests;

/// <summary>
/// Integration tests for settings persistence and reload functionality
/// </summary>
public class SettingsIntegrationTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _testSettingsFile;

    public SettingsIntegrationTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"HAVolumeSync_IntegrationTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _testSettingsFile = Path.Combine(_testDirectory, "appsettings.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void SettingsSaveAndReload_UpdatesConfigurationValues()
    {
        // Arrange - Create initial settings file
        var initialJson = @"{
  ""HomeAssistant"": {
    ""WebhookUrl"": ""https://initial.local"",
    ""WebhookId"": ""initial_webhook"",
    ""TargetMediaPlayer"": ""media_player.initial""
  }
}";
        File.WriteAllText(_testSettingsFile, initialJson);

        // Build configuration with reload enabled
        var configuration = new ConfigurationBuilder()
            .SetBasePath(_testDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var appConfig = new AppConfiguration(configuration);
        var settingsManager = new SettingsManager(_testSettingsFile, appConfig, null);

        // Verify initial values
        Assert.Equal("https://initial.local", appConfig.WebhookUrl);
        Assert.Equal("initial_webhook", appConfig.WebhookId);
        Assert.Equal("media_player.initial", appConfig.TargetMediaPlayer);

        // Track if configuration reloaded event fires
        var reloadEventFired = false;
        appConfig.ConfigurationReloaded += (sender, e) => reloadEventFired = true;

        // Act - Save new settings
        settingsManager.SaveSettings("https://updated.local", "updated_webhook", "media_player.updated");

        // Small delay to allow file watcher to detect changes
        Thread.Sleep(500);

        // Assert - Configuration should be reloaded with new values
        Assert.True(reloadEventFired, "Configuration reload event should have fired");
        Assert.Equal("https://updated.local", appConfig.WebhookUrl);
        Assert.Equal("updated_webhook", appConfig.WebhookId);
        Assert.Equal("media_player.updated", appConfig.TargetMediaPlayer);
    }

    [Fact]
    public void SettingsSaveAndReload_UpdatesFullWebhookUrl()
    {
        // Arrange
        var initialJson = @"{
  ""HomeAssistant"": {
    ""WebhookUrl"": ""https://initial.local"",
    ""WebhookPath"": ""/api/webhook/"",
    ""WebhookId"": ""initial_webhook"",
    ""TargetMediaPlayer"": ""media_player.initial""
  }
}";
        File.WriteAllText(_testSettingsFile, initialJson);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(_testDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var appConfig = new AppConfiguration(configuration);
        var settingsManager = new SettingsManager(_testSettingsFile, appConfig, null);

        var initialFullUrl = appConfig.FullWebhookUrl;
        Assert.Equal("https://initial.local/api/webhook/initial_webhook", initialFullUrl);

        // Act
        settingsManager.SaveSettings("https://updated.local", "updated_webhook", "media_player.updated");
        Thread.Sleep(500);

        // Assert
        var updatedFullUrl = appConfig.FullWebhookUrl;
        Assert.Equal("https://updated.local/api/webhook/updated_webhook", updatedFullUrl);
    }

    [Fact]
    public void ConfigurationReload_TriggersSubscribedServices()
    {
        // Arrange
        var initialJson = @"{
  ""HomeAssistant"": {
    ""WebhookUrl"": ""https://initial.local"",
    ""WebhookId"": ""initial_webhook"",
    ""TargetMediaPlayer"": ""media_player.initial""
  }
}";
        File.WriteAllText(_testSettingsFile, initialJson);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(_testDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var appConfig = new AppConfiguration(configuration);
        var settingsManager = new SettingsManager(_testSettingsFile, appConfig, null);

        // Simulate a service subscribing to configuration changes
        string? capturedWebhookUrl = null;
        string? capturedWebhookId = null;

        appConfig.ConfigurationReloaded += (sender, e) =>
        {
            capturedWebhookUrl = appConfig.WebhookUrl;
            capturedWebhookId = appConfig.WebhookId;
        };

        // Act
        settingsManager.SaveSettings("https://new.local", "new_webhook", "media_player.new");
        Thread.Sleep(500);

        // Assert
        Assert.Equal("https://new.local", capturedWebhookUrl);
        Assert.Equal("new_webhook", capturedWebhookId);
    }
}
