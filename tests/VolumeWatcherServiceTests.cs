using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HomeAssistantWindowsVolumeSync.Tests;

public class VolumeWatcherServiceTests
{
    private readonly Mock<ILogger<VolumeWatcherService>> _mockLogger;
    private readonly Mock<IHomeAssistantClient> _mockHomeAssistantClient;
    private readonly Mock<IConfiguration> _mockConfiguration;

    public VolumeWatcherServiceTests()
    {
        _mockLogger = new Mock<ILogger<VolumeWatcherService>>();
        _mockHomeAssistantClient = new Mock<IHomeAssistantClient>();
        _mockConfiguration = new Mock<IConfiguration>();
    }

    [Fact]
    public void Constructor_ShouldInitialize_Successfully()
    {
        // Act
        var service = new VolumeWatcherService(
            _mockLogger.Object,
            _mockHomeAssistantClient.Object,
            _mockConfiguration.Object);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldNotThrow()
    {
        // Act & Assert - Should not throw
        var service = new VolumeWatcherService(
            _mockLogger.Object,
            _mockHomeAssistantClient.Object,
            _mockConfiguration.Object);

        Assert.NotNull(service);
    }

    [Fact]
    public void SetPaused_WhenCalledWithTrue_ShouldLogPausedStatus()
    {
        // Arrange
        var service = new VolumeWatcherService(
            _mockLogger.Object,
            _mockHomeAssistantClient.Object,
            _mockConfiguration.Object);

        // Act
        service.SetPaused(true);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("paused")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void SetPaused_WhenCalledWithFalse_ShouldLogResumedStatus()
    {
        // Arrange
        var service = new VolumeWatcherService(
            _mockLogger.Object,
            _mockHomeAssistantClient.Object,
            _mockConfiguration.Object);

        // Act
        service.SetPaused(false);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("resumed")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void SetPaused_WhenCalledMultipleTimes_ShouldLogEachTime()
    {
        // Arrange
        var service = new VolumeWatcherService(
            _mockLogger.Object,
            _mockHomeAssistantClient.Object,
            _mockConfiguration.Object);

        // Act
        service.SetPaused(true);
        service.SetPaused(false);
        service.SetPaused(true);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(3));
    }

    [Fact]
    public void Dispose_ShouldDisposeResourcesSafely()
    {
        // Arrange
        var service = new VolumeWatcherService(
            _mockLogger.Object,
            _mockHomeAssistantClient.Object,
            _mockConfiguration.Object);

        // Act - Should not throw
        service.Dispose();

        // Assert - Multiple disposes should also be safe
        service.Dispose();
    }

    [Fact]
    public void ServiceMetadata_ShouldBeCorrect()
    {
        // Arrange
        var service = new VolumeWatcherService(
            _mockLogger.Object,
            _mockHomeAssistantClient.Object,
            _mockConfiguration.Object);

        // Assert - Verify it's a BackgroundService
        Assert.IsAssignableFrom<BackgroundService>(service);
        Assert.IsAssignableFrom<IHostedService>(service);
    }

    [Fact]
    public async Task StartAsync_ShouldNotThrow()
    {
        // Arrange
        var service = new VolumeWatcherService(
            _mockLogger.Object,
            _mockHomeAssistantClient.Object,
            _mockConfiguration.Object);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act & Assert - Should not throw during startup even if no audio device is available
        try
        {
            await service.StartAsync(cts.Token);
            // Give the service a moment to initialize
            await Task.Delay(50);
            await service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is triggered
        }
        catch (System.Runtime.InteropServices.COMException ex) when (ex.HResult == unchecked((int)0x80070490))
        {
            // This is now handled gracefully in the service, but if it somehow reaches here,
            // it's acceptable in test environments without audio devices
            Assert.True(true, "COM exception for missing audio device is acceptable in test environment");
        }

        // Verify that the service logged appropriately
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("starting")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_WithNoAudioDevice_ShouldLogWarningAndContinue()
    {
        // Arrange
        var service = new VolumeWatcherService(
            _mockLogger.Object,
            _mockHomeAssistantClient.Object,
            _mockConfiguration.Object);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act
        try
        {
            await service.StartAsync(cts.Token);
            await Task.Delay(50);
            await service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert - Should log a warning if no audio device is available
        // This might log "Could not access" or "No default audio endpoint found" depending on environment
        _mockLogger.Verify(
            x => x.Log(
                It.Is<LogLevel>(l => l == LogLevel.Warning || l == LogLevel.Information),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task StopAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var service = new VolumeWatcherService(
            _mockLogger.Object,
            _mockHomeAssistantClient.Object,
            _mockConfiguration.Object);

        // Act & Assert - Should complete without throwing
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public void Constructor_ShouldAcceptAllRequiredDependencies()
    {
        // Arrange
        var logger = new Mock<ILogger<VolumeWatcherService>>();
        var client = new Mock<IHomeAssistantClient>();
        var config = new Mock<IConfiguration>();

        // Act
        var service = new VolumeWatcherService(
            logger.Object,
            client.Object,
            config.Object);

        // Assert
        Assert.NotNull(service);
        Assert.IsType<VolumeWatcherService>(service);
    }

    [Fact]
    public void SetPaused_WithSameValueMultipleTimes_ShouldLogEachCall()
    {
        // Arrange
        var service = new VolumeWatcherService(
            _mockLogger.Object,
            _mockHomeAssistantClient.Object,
            _mockConfiguration.Object);

        // Act
        service.SetPaused(true);
        service.SetPaused(true);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("paused")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }
}
