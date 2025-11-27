using Moq;
using Xunit;

namespace HomeAssistantWindowsVolumeSync.Tests;

public class SettingsManagerTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly string _testSettingsFile;
    private readonly Mock<IAppConfiguration> _mockConfiguration;

    public SettingsManagerTests()
    {
        // Create a unique test directory for each test run
        _testDirectory = Path.Combine(Path.GetTempPath(), $"SettingsManagerTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
        _testSettingsFile = Path.Combine(_testDirectory, "appsettings.json");
        _mockConfiguration = new Mock<IAppConfiguration>();
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void Constructor_WithoutConfiguration_DoesNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => new SettingsManager(null));
        Assert.Null(exception);
    }

    [Fact]
    public void Constructor_WithConfiguration_DoesNotThrow()
    {
        // Arrange
        var mockConfig = new Mock<IAppConfiguration>();

        // Act & Assert
        var exception = Record.Exception(() => new SettingsManager(mockConfig.Object));
        Assert.Null(exception);
    }

    [Fact]
    public void SaveSettings_CreatesNewFile_WhenFileDoesNotExist()
    {
        // Arrange
        var settingsManager = CreateSettingsManager();
        var webhookUrl = "https://test.local";
        var webhookId = "test_webhook";
        var targetMediaPlayer = "media_player.test";

        // Act
        settingsManager.SaveSettings(webhookUrl, webhookId, targetMediaPlayer);

        // Assert
        Assert.True(File.Exists(_testSettingsFile));
    }

    [Fact]
    public void SaveSettings_CreatesHomeAssistantSection_WhenFileIsEmpty()
    {
        // Arrange
        File.WriteAllText(_testSettingsFile, "{}");
        var settingsManager = CreateSettingsManager();
        var webhookUrl = "https://test.local";
        var webhookId = "test_webhook";
        var targetMediaPlayer = "media_player.test";

        // Act
        settingsManager.SaveSettings(webhookUrl, webhookId, targetMediaPlayer);

        // Assert
        var json = File.ReadAllText(_testSettingsFile);
        Assert.Contains("HomeAssistant", json);
        Assert.Contains(webhookUrl, json);
        Assert.Contains(webhookId, json);
        Assert.Contains(targetMediaPlayer, json);
    }

    [Fact]
    public void SaveSettings_UpdatesExistingHomeAssistantSection_PreservingOtherSettings()
    {
        // Arrange
        var initialJson = @"{
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Information""
    }
  },
  ""HomeAssistant"": {
    ""WebhookUrl"": ""https://old.local"",
    ""WebhookPath"": ""/api/webhook/"",
    ""WebhookId"": ""old_webhook"",
    ""TargetMediaPlayer"": ""media_player.old"",
    ""StrictTLS"": true
  }
}";
        File.WriteAllText(_testSettingsFile, initialJson);
        var settingsManager = CreateSettingsManager();
        var webhookUrl = "https://new.local";
        var webhookId = "new_webhook";
        var targetMediaPlayer = "media_player.new";

        // Act
        settingsManager.SaveSettings(webhookUrl, webhookId, targetMediaPlayer);

        // Assert
        var json = File.ReadAllText(_testSettingsFile);
        Assert.Contains("Logging", json);
        Assert.Contains(webhookUrl, json);
        Assert.Contains(webhookId, json);
        Assert.Contains(targetMediaPlayer, json);
        Assert.DoesNotContain("old.local", json);
        Assert.DoesNotContain("old_webhook", json);
        Assert.DoesNotContain("media_player.old", json);
    }

    [Fact]
    public void SaveSettings_PreservesOtherHomeAssistantSettings()
    {
        // Arrange
        var initialJson = @"{
  ""HomeAssistant"": {
    ""WebhookUrl"": ""https://old.local"",
    ""WebhookPath"": ""/custom/path/"",
    ""WebhookId"": ""old_webhook"",
    ""TargetMediaPlayer"": ""media_player.old"",
    ""StrictTLS"": false
  }
}";
        File.WriteAllText(_testSettingsFile, initialJson);
        var settingsManager = CreateSettingsManager();
        var webhookUrl = "https://new.local";
        var webhookId = "new_webhook";
        var targetMediaPlayer = "media_player.new";

        // Act
        settingsManager.SaveSettings(webhookUrl, webhookId, targetMediaPlayer);

        // Assert
        var json = File.ReadAllText(_testSettingsFile);
        Assert.Contains("\"/custom/path/\"", json);
        Assert.Contains("false", json.ToLower());
    }

    [Fact]
    public void SaveSettings_WritesIndentedJson()
    {
        // Arrange
        File.WriteAllText(_testSettingsFile, "{}");
        var settingsManager = CreateSettingsManager();

        // Act
        settingsManager.SaveSettings("https://test.local", "test_webhook", "media_player.test");

        // Assert
        var json = File.ReadAllText(_testSettingsFile);
        Assert.Contains(Environment.NewLine, json);
        Assert.Contains("  ", json); // Should have indentation
    }

    [Fact]
    public void SaveSettings_CallsReload_WhenConfigurationIsProvided()
    {
        // Arrange
        File.WriteAllText(_testSettingsFile, "{}");
        var settingsManager = CreateSettingsManager();

        // Act
        settingsManager.SaveSettings("https://test.local", "test_webhook", "media_player.test");

        // Assert
        _mockConfiguration.Verify(c => c.Reload(), Times.Once);
    }

    [Fact]
    public void SaveSettings_DoesNotCallReload_WhenConfigurationIsNull()
    {
        // Arrange
        File.WriteAllText(_testSettingsFile, "{}");
        var settingsManager = CreateSettingsManagerWithoutConfig();

        // Act & Assert - Should not throw
        var exception = Record.Exception(() =>
            settingsManager.SaveSettings("https://test.local", "test_webhook", "media_player.test"));
        Assert.Null(exception);
    }

    [Fact]
    public void SaveSettings_HandlesSpecialCharacters()
    {
        // Arrange
        File.WriteAllText(_testSettingsFile, "{}");
        var settingsManager = CreateSettingsManager();
        var webhookUrl = "https://test.local/with-special-chars?key=value&test=123";
        var webhookId = "webhook_with_underscores-and-dashes";
        var targetMediaPlayer = "media_player.special_chars_123";

        // Act
        settingsManager.SaveSettings(webhookUrl, webhookId, targetMediaPlayer);

        // Assert
        var json = File.ReadAllText(_testSettingsFile);
        Assert.Contains(webhookUrl, json);
        Assert.Contains(webhookId, json);
        Assert.Contains(targetMediaPlayer, json);
    }

    [Fact]
    public void SaveSettings_HandlesEmptyStrings()
    {
        // Arrange
        File.WriteAllText(_testSettingsFile, "{}");
        var settingsManager = CreateSettingsManager();

        // Act
        settingsManager.SaveSettings("", "", "");

        // Assert
        var json = File.ReadAllText(_testSettingsFile);
        Assert.Contains("HomeAssistant", json);
        // Empty strings should be preserved
        Assert.Contains("\"\"", json);
    }

    [Fact]
    public void SaveSettings_PreservesMultipleSections()
    {
        // Arrange
        var initialJson = @"{
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Information"",
      ""Microsoft"": ""Warning""
    }
  },
  ""CustomSection"": {
    ""Key1"": ""Value1"",
    ""Key2"": ""Value2""
  },
  ""HomeAssistant"": {
    ""WebhookUrl"": ""https://old.local"",
    ""WebhookId"": ""old_webhook"",
    ""TargetMediaPlayer"": ""media_player.old""
  }
}";
        File.WriteAllText(_testSettingsFile, initialJson);
        var settingsManager = CreateSettingsManager();

        // Act
        settingsManager.SaveSettings("https://new.local", "new_webhook", "media_player.new");

        // Assert
        var json = File.ReadAllText(_testSettingsFile);
        Assert.Contains("Logging", json);
        Assert.Contains("CustomSection", json);
        Assert.Contains("Key1", json);
        Assert.Contains("Value1", json);
        Assert.Contains("https://new.local", json);
    }

    [Fact]
    public void SaveSettings_CreatesDefaultHomeAssistantSettings_WhenSectionIsMissing()
    {
        // Arrange
        var initialJson = @"{
  ""Logging"": {
    ""LogLevel"": {
      ""Default"": ""Information""
    }
  }
}";
        File.WriteAllText(_testSettingsFile, initialJson);
        var settingsManager = CreateSettingsManager();

        // Act
        settingsManager.SaveSettings("https://test.local", "test_webhook", "media_player.test");

        // Assert
        var json = File.ReadAllText(_testSettingsFile);
        Assert.Contains("HomeAssistant", json);
        Assert.Contains("\"/api/webhook/\"", json);
        Assert.Contains("StrictTLS", json);
        Assert.Contains("true", json.ToLower());
    }

    private SettingsManager CreateSettingsManager()
    {
        return new SettingsManager(_testSettingsFile, _mockConfiguration.Object);
    }

    private SettingsManager CreateSettingsManagerWithoutConfig()
    {
        return new SettingsManager(_testSettingsFile, null);
    }
}
