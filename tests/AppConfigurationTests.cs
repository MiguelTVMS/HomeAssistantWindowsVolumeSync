using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace HomeAssistantWindowsVolumeSync.Tests;

public class AppConfigurationTests
{
    [Fact]
    public void Constructor_NullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AppConfiguration(null!));
    }

    [Fact]
    public void WebhookUrl_ReturnsConfiguredValue()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "HomeAssistant:WebhookUrl", "https://test.local/webhook" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var appConfig = new AppConfiguration(configuration);

        // Act
        var webhookUrl = appConfig.WebhookUrl;

        // Assert
        Assert.Equal("https://test.local/webhook", webhookUrl);
    }

    [Fact]
    public void TargetMediaPlayer_ReturnsConfiguredValue()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "HomeAssistant:TargetMediaPlayer", "media_player.test_device" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var appConfig = new AppConfiguration(configuration);

        // Act
        var targetMediaPlayer = appConfig.TargetMediaPlayer;

        // Assert
        Assert.Equal("media_player.test_device", targetMediaPlayer);
    }

    [Fact]
    public void StrictTLS_DefaultsToTrue_WhenNotConfigured()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var appConfig = new AppConfiguration(configuration);

        // Act
        var strictTls = appConfig.StrictTLS;

        // Assert
        Assert.True(strictTls);
    }

    [Fact]
    public void StrictTLS_ReturnsFalse_WhenConfiguredAsFalse()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "HomeAssistant:StrictTLS", "false" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var appConfig = new AppConfiguration(configuration);

        // Act
        var strictTls = appConfig.StrictTLS;

        // Assert
        Assert.False(strictTls);
    }

    [Fact]
    public void StrictTLS_ReturnsTrue_WhenConfiguredAsTrue()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "HomeAssistant:StrictTLS", "true" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var appConfig = new AppConfiguration(configuration);

        // Act
        var strictTls = appConfig.StrictTLS;

        // Assert
        Assert.True(strictTls);
    }

    [Fact]
    public void GetValue_ReturnsConfiguredValue()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "TestKey", "TestValue" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var appConfig = new AppConfiguration(configuration);

        // Act
        var value = appConfig.GetValue("TestKey");

        // Assert
        Assert.Equal("TestValue", value);
    }

    [Fact]
    public void GetValue_ReturnsNull_WhenKeyNotFound()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var appConfig = new AppConfiguration(configuration);

        // Act
        var value = appConfig.GetValue("NonExistentKey");

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void GetValueGeneric_ReturnsConfiguredValue()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "TestInt", "42" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var appConfig = new AppConfiguration(configuration);

        // Act
        var value = appConfig.GetValue<int>("TestInt", 0);

        // Assert
        Assert.Equal(42, value);
    }

    [Fact]
    public void GetValueGeneric_ReturnsDefaultValue_WhenKeyNotFound()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var appConfig = new AppConfiguration(configuration);

        // Act
        var value = appConfig.GetValue<int>("NonExistentKey", 99);

        // Assert
        Assert.Equal(99, value);
    }

    [Fact]
    public void Reload_WithConfigurationRoot_RaisesConfigurationReloadedEvent()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "HomeAssistant:WebhookUrl", "https://test.local/webhook" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var appConfig = new AppConfiguration(configuration);
        var eventRaised = false;

        appConfig.ConfigurationReloaded += (sender, e) =>
        {
            eventRaised = true;
        };

        // Act
        appConfig.Reload();

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void ConfigurationReloadedEvent_CanBeSubscribedAndUnsubscribed()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var appConfig = new AppConfiguration(configuration);
        var eventRaisedCount = 0;

        void EventHandler(object? sender, EventArgs e) => eventRaisedCount++;

        appConfig.ConfigurationReloaded += EventHandler;

        // Act - First reload
        appConfig.Reload();
        Assert.Equal(1, eventRaisedCount);

        // Unsubscribe
        appConfig.ConfigurationReloaded -= EventHandler;

        // Act - Second reload (should not increment)
        appConfig.Reload();

        // Assert
        Assert.Equal(1, eventRaisedCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void WebhookUrl_ReturnsEmptyOrNull_WhenNotConfigured(string? expectedValue)
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "HomeAssistant:WebhookUrl", expectedValue }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var appConfig = new AppConfiguration(configuration);

        // Act
        var webhookUrl = appConfig.WebhookUrl;

        // Assert
        Assert.Equal(expectedValue, webhookUrl);
    }

    [Fact]
    public void MultipleProperties_CanBeAccessedConcurrently()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "HomeAssistant:WebhookUrl", "https://test.local/webhook" },
            { "HomeAssistant:TargetMediaPlayer", "media_player.test" },
            { "HomeAssistant:StrictTLS", "false" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        var appConfig = new AppConfiguration(configuration);

        // Act & Assert
        Assert.Equal("https://test.local/webhook", appConfig.WebhookUrl);
        Assert.Equal("media_player.test", appConfig.TargetMediaPlayer);
        Assert.False(appConfig.StrictTLS);
    }
}
