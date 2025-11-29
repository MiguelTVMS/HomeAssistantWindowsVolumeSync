using System.Diagnostics;
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
    
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);
    private static readonly int PollingIntervalMs = 50;

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

    /// <summary>
    /// Waits for a condition to become true with a configurable timeout.
    /// Uses polling instead of Thread.Sleep for more reliable test behavior.
    /// </summary>
    private static bool WaitForCondition(Func<bool> condition, TimeSpan? timeout = null)
    {
        var actualTimeout = timeout ?? DefaultTimeout;
        var stopwatch = Stopwatch.StartNew();
        while (!condition() && stopwatch.Elapsed < actualTimeout)
        {
            Thread.Sleep(PollingIntervalMs);
        }
        return condition();
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

        // Wait for reload with timeout (polling approach for reliability)
        WaitForCondition(() => reloadEventFired);

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
        var expectedUrl = "https://updated.local/api/webhook/updated_webhook";
        settingsManager.SaveSettings("https://updated.local", "updated_webhook", "media_player.updated");
        
        // Wait for configuration to update with timeout (polling approach for reliability)
        WaitForCondition(() => appConfig.FullWebhookUrl == expectedUrl);

        // Assert
        var updatedFullUrl = appConfig.FullWebhookUrl;
        Assert.Equal(expectedUrl, updatedFullUrl);
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
        
        // Wait for configuration to reload with timeout (polling approach for reliability)
        WaitForCondition(() => capturedWebhookUrl != null);

        // Assert
        Assert.Equal("https://new.local", capturedWebhookUrl);
        Assert.Equal("new_webhook", capturedWebhookId);
    }
}
