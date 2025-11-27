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

        // Act & Assert - Should not throw during startup
        try
        {
            await service.StartAsync(cts.Token);
            cts.Cancel();
            await service.StopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is triggered
        }
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
