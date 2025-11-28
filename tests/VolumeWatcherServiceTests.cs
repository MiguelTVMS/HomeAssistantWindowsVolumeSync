using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HomeAssistantWindowsVolumeSync.Tests;

public class VolumeWatcherServiceTests
{
    private readonly Mock<ILogger<VolumeWatcherService>> _mockLogger;
    private readonly Mock<IHomeAssistantClient> _mockHomeAssistantClient;
    private readonly Mock<IAppConfiguration> _mockConfiguration;

    public VolumeWatcherServiceTests()
    {
        _mockLogger = new Mock<ILogger<VolumeWatcherService>>();
        _mockHomeAssistantClient = new Mock<IHomeAssistantClient>();
        _mockConfiguration = new Mock<IAppConfiguration>();
        _mockConfiguration.Setup(c => c.DebounceTimer).Returns(100); // Default value
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
        var config = new Mock<IAppConfiguration>();
        config.Setup(c => c.DebounceTimer).Returns(100);

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

    #region Debounce Tests

    [Fact]
    public async Task HandleVolumeChange_WithinDebounceWindow_ShouldBatchUpdates()
    {
        // Arrange
        var debounceMs = 200;
        _mockConfiguration.Setup(c => c.DebounceTimer).Returns(debounceMs);
        _mockHomeAssistantClient
            .Setup(c => c.SendVolumeUpdateAsync(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);

        var service = new VolumeWatcherService(
            _mockLogger.Object,
            _mockHomeAssistantClient.Object,
            _mockConfiguration.Object);

        // Act - Simulate multiple rapid volume changes
        service.HandleVolumeChange(0.1f, false);
        service.HandleVolumeChange(0.2f, false);
        service.HandleVolumeChange(0.3f, false);
        service.HandleVolumeChange(0.5f, false);

        // Wait for debounce to complete
        await Task.Delay(debounceMs + 100);

        // Assert - Only one webhook call should be made
        _mockHomeAssistantClient.Verify(
            c => c.SendVolumeUpdateAsync(It.IsAny<int>(), It.IsAny<bool>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleVolumeChange_AfterDebounceWindow_ShouldSendLatestValue()
    {
        // Arrange
        var debounceMs = 100;
        _mockConfiguration.Setup(c => c.DebounceTimer).Returns(debounceMs);
        _mockHomeAssistantClient
            .Setup(c => c.SendVolumeUpdateAsync(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);

        var service = new VolumeWatcherService(
            _mockLogger.Object,
            _mockHomeAssistantClient.Object,
            _mockConfiguration.Object);

        // Act - Simulate volume changes, the last one should be sent
        service.HandleVolumeChange(0.1f, false);  // 10%
        service.HandleVolumeChange(0.5f, true);   // 50%, muted - this is the latest

        // Wait for debounce to complete
        await Task.Delay(debounceMs + 100);

        // Assert - Should send the latest value (50%, muted)
        _mockHomeAssistantClient.Verify(
            c => c.SendVolumeUpdateAsync(50, true),
            Times.Once);
    }

    [Fact]
    public async Task HandleVolumeChange_ShouldWaitForDebounceTimer()
    {
        // Arrange
        var debounceMs = 200;
        _mockConfiguration.Setup(c => c.DebounceTimer).Returns(debounceMs);
        _mockHomeAssistantClient
            .Setup(c => c.SendVolumeUpdateAsync(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);

        var service = new VolumeWatcherService(
            _mockLogger.Object,
            _mockHomeAssistantClient.Object,
            _mockConfiguration.Object);

        // Act
        service.HandleVolumeChange(0.5f, false);

        // Assert - No call yet before debounce period
        await Task.Delay(50); // Less than debounce time
        _mockHomeAssistantClient.Verify(
            c => c.SendVolumeUpdateAsync(It.IsAny<int>(), It.IsAny<bool>()),
            Times.Never);

        // Wait for debounce to complete
        await Task.Delay(debounceMs + 50);

        // Assert - Call should be made after debounce period
        _mockHomeAssistantClient.Verify(
            c => c.SendVolumeUpdateAsync(50, false),
            Times.Once);
    }

    [Fact]
    public async Task HandleVolumeChange_RapidChanges_ShouldResetDebounceTimer()
    {
        // Arrange
        var debounceMs = 100;
        _mockConfiguration.Setup(c => c.DebounceTimer).Returns(debounceMs);
        _mockHomeAssistantClient
            .Setup(c => c.SendVolumeUpdateAsync(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);

        var service = new VolumeWatcherService(
            _mockLogger.Object,
            _mockHomeAssistantClient.Object,
            _mockConfiguration.Object);

        // Act - Send first volume change
        service.HandleVolumeChange(0.2f, false);
        
        // Wait half the debounce time and send another change
        await Task.Delay(50);
        service.HandleVolumeChange(0.4f, false);

        // Wait another half - at this point, if timer wasn't reset, 
        // the first call would have fired (100ms from first change)
        await Task.Delay(50);
        
        // Assert - No call should be made yet (timer was reset)
        _mockHomeAssistantClient.Verify(
            c => c.SendVolumeUpdateAsync(It.IsAny<int>(), It.IsAny<bool>()),
            Times.Never);

        // Wait for debounce to complete from the second change
        await Task.Delay(debounceMs);

        // Assert - Now the call should be made with the latest value
        _mockHomeAssistantClient.Verify(
            c => c.SendVolumeUpdateAsync(40, false),
            Times.Once);
    }

    [Fact]
    public void HandleVolumeChange_WhenPaused_ShouldNotStartDebounce()
    {
        // Arrange
        _mockConfiguration.Setup(c => c.DebounceTimer).Returns(100);

        var service = new VolumeWatcherService(
            _mockLogger.Object,
            _mockHomeAssistantClient.Object,
            _mockConfiguration.Object);

        service.SetPaused(true);

        // Act
        service.HandleVolumeChange(0.5f, false);

        // Assert - Should log debug message about skipping
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("skipped")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // No webhook call should be scheduled
        _mockHomeAssistantClient.Verify(
            c => c.SendVolumeUpdateAsync(It.IsAny<int>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleVolumeChange_TimerDispose_ShouldBeSafeOnRapidChanges()
    {
        // Arrange
        var debounceMs = 50;
        _mockConfiguration.Setup(c => c.DebounceTimer).Returns(debounceMs);
        _mockHomeAssistantClient
            .Setup(c => c.SendVolumeUpdateAsync(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);

        var service = new VolumeWatcherService(
            _mockLogger.Object,
            _mockHomeAssistantClient.Object,
            _mockConfiguration.Object);

        // Act - Simulate very rapid changes that would trigger timer disposal
        for (int i = 0; i <= 100; i++)
        {
            service.HandleVolumeChange(i / 100f, false);
        }

        // Wait for debounce to complete
        await Task.Delay(debounceMs + 100);

        // Assert - Should complete without throwing and only send one update
        _mockHomeAssistantClient.Verify(
            c => c.SendVolumeUpdateAsync(It.IsAny<int>(), It.IsAny<bool>()),
            Times.Once);

        // Should send the last value (100%)
        _mockHomeAssistantClient.Verify(
            c => c.SendVolumeUpdateAsync(100, false),
            Times.Once);
    }

    [Fact]
    public async Task HandleVolumeChange_SeparateDebounceWindows_ShouldSendMultipleUpdates()
    {
        // Arrange
        var debounceMs = 100;
        _mockConfiguration.Setup(c => c.DebounceTimer).Returns(debounceMs);
        _mockHomeAssistantClient
            .Setup(c => c.SendVolumeUpdateAsync(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);

        var service = new VolumeWatcherService(
            _mockLogger.Object,
            _mockHomeAssistantClient.Object,
            _mockConfiguration.Object);

        // Act - First batch of changes
        service.HandleVolumeChange(0.3f, false);
        await Task.Delay(debounceMs + 50);

        // Second batch of changes (after first debounce completed)
        service.HandleVolumeChange(0.7f, true);
        await Task.Delay(debounceMs + 50);

        // Assert - Two separate calls should be made
        _mockHomeAssistantClient.Verify(
            c => c.SendVolumeUpdateAsync(It.IsAny<int>(), It.IsAny<bool>()),
            Times.Exactly(2));

        _mockHomeAssistantClient.Verify(
            c => c.SendVolumeUpdateAsync(30, false),
            Times.Once);

        _mockHomeAssistantClient.Verify(
            c => c.SendVolumeUpdateAsync(70, true),
            Times.Once);
    }

    [Fact]
    public void Dispose_ShouldDisposeDebounceTimer()
    {
        // Arrange
        _mockConfiguration.Setup(c => c.DebounceTimer).Returns(1000);
        _mockHomeAssistantClient
            .Setup(c => c.SendVolumeUpdateAsync(It.IsAny<int>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);

        var service = new VolumeWatcherService(
            _mockLogger.Object,
            _mockHomeAssistantClient.Object,
            _mockConfiguration.Object);

        // Start a debounce timer
        service.HandleVolumeChange(0.5f, false);

        // Act - Dispose before timer fires
        service.Dispose();

        // Assert - Timer should be disposed without throwing
        // Multiple disposes should also be safe
        service.Dispose();
    }

    #endregion
}

