using Microsoft.Extensions.Configuration;
using Xunit;

namespace HomeAssistantWindowsVolumeSync.Tests;

/// <summary>
/// Contract tests for IAppConfiguration interface to ensure implementations follow expected behavior.
/// </summary>
public class IAppConfigurationTests
{
    [Fact]
    public void IAppConfiguration_HasWebhookUrlProperty()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "HomeAssistant:WebhookUrl", "https://test.local/webhook" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        IAppConfiguration appConfig = new AppConfiguration(configuration);

        // Act & Assert
        Assert.NotNull(appConfig.WebhookUrl);
    }

    [Fact]
    public void IAppConfiguration_HasTargetMediaPlayerProperty()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "HomeAssistant:TargetMediaPlayer", "media_player.test" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        IAppConfiguration appConfig = new AppConfiguration(configuration);

        // Act & Assert
        Assert.NotNull(appConfig.TargetMediaPlayer);
    }

    [Fact]
    public void IAppConfiguration_HasStrictTLSProperty()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        IAppConfiguration appConfig = new AppConfiguration(configuration);

        // Act & Assert - Should have a default value (it's a value type, always has a value)
        var strictTLS = appConfig.StrictTLS;
        Assert.True(strictTLS); // Default is true
    }

    [Fact]
    public void IAppConfiguration_HasReloadMethod()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        IAppConfiguration appConfig = new AppConfiguration(configuration);

        // Act & Assert - Method should exist and be callable
        appConfig.Reload();
    }

    [Fact]
    public void IAppConfiguration_HasGetValueMethod()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "TestKey", "TestValue" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        IAppConfiguration appConfig = new AppConfiguration(configuration);

        // Act
        var value = appConfig.GetValue("TestKey");

        // Assert
        Assert.Equal("TestValue", value);
    }

    [Fact]
    public void IAppConfiguration_HasGetValueGenericMethod()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            { "TestInt", "42" }
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        IAppConfiguration appConfig = new AppConfiguration(configuration);

        // Act
        var value = appConfig.GetValue<int>("TestInt", 0);

        // Assert
        Assert.Equal(42, value);
    }

    [Fact]
    public void IAppConfiguration_HasConfigurationReloadedEvent()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        IAppConfiguration appConfig = new AppConfiguration(configuration);
        var eventRaised = false;

        // Act
        appConfig.ConfigurationReloaded += (sender, e) => eventRaised = true;
        appConfig.Reload();

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public void IAppConfiguration_CanBeUsedThroughInterface()
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

        IAppConfiguration appConfig = new AppConfiguration(configuration);

        // Act & Assert - Use through interface reference
        UseConfiguration(appConfig);
    }

    private static void UseConfiguration(IAppConfiguration config)
    {
        Assert.NotNull(config.WebhookUrl);
        Assert.NotNull(config.TargetMediaPlayer);
        // StrictTLS is bool, always has a value
        _ = config.StrictTLS;
    }

    [Fact]
    public void IAppConfiguration_Implementation_IsReusable()
    {
        // Arrange
        var configData1 = new Dictionary<string, string?>
        {
            { "HomeAssistant:WebhookUrl", "https://test1.local/webhook" }
        };
        var configuration1 = new ConfigurationBuilder()
            .AddInMemoryCollection(configData1)
            .Build();

        var configData2 = new Dictionary<string, string?>
        {
            { "HomeAssistant:WebhookUrl", "https://test2.local/webhook" }
        };
        var configuration2 = new ConfigurationBuilder()
            .AddInMemoryCollection(configData2)
            .Build();

        // Act - Create multiple instances
        IAppConfiguration appConfig1 = new AppConfiguration(configuration1);
        IAppConfiguration appConfig2 = new AppConfiguration(configuration2);

        // Assert - Both instances should work independently
        Assert.Equal("https://test1.local/webhook", appConfig1.WebhookUrl);
        Assert.Equal("https://test2.local/webhook", appConfig2.WebhookUrl);
    }
}
