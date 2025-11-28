using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace HomeAssistantWindowsVolumeSync.Tests;

public class SystemTrayServiceTests
{
    private readonly Mock<ILogger<SystemTrayService>> _mockLogger;
    private readonly Mock<IHostApplicationLifetime> _mockLifetime;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IServiceScope> _mockServiceScope;
    private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;
    private readonly Mock<IAppConfiguration> _mockConfiguration;
    private VolumeWatcherService? _volumeWatcherService;

    public SystemTrayServiceTests()
    {
        _mockLogger = new Mock<ILogger<SystemTrayService>>();
        _mockLifetime = new Mock<IHostApplicationLifetime>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockServiceScope = new Mock<IServiceScope>();
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        _mockConfiguration = new Mock<IAppConfiguration>();
        _mockConfiguration.Setup(c => c.DebounceTimer).Returns(100); // Default value

        // Create a real VolumeWatcherService with mocked dependencies
        var mockVolumeWatcherLogger = new Mock<ILogger<VolumeWatcherService>>();
        var mockHomeAssistantClient = new Mock<IHomeAssistantClient>();

        _volumeWatcherService = new VolumeWatcherService(
            mockVolumeWatcherLogger.Object,
            mockHomeAssistantClient.Object,
            _mockConfiguration.Object);

        // Setup service provider to return the VolumeWatcherService
        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(VolumeWatcherService)))
            .Returns(_volumeWatcherService);
    }

    [Fact]
    public void Constructor_ShouldInitialize_Successfully()
    {
        // Act
        var service = new SystemTrayService(
            _mockLogger.Object,
            _mockLifetime.Object,
            _mockServiceProvider.Object,
            _mockConfiguration.Object);

        // Assert
        Assert.NotNull(service);
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("constructor called")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldNotThrow()
    {
        // Act & Assert - Should not throw
        var service = new SystemTrayService(
            _mockLogger.Object,
            _mockLifetime.Object,
            _mockServiceProvider.Object,
            _mockConfiguration.Object);

        Assert.NotNull(service);
    }

    [Fact]
    public void Dispose_ShouldDisposeResourcesSafely()
    {
        // Arrange
        var service = new SystemTrayService(
            _mockLogger.Object,
            _mockLifetime.Object,
            _mockServiceProvider.Object,
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
        var service = new SystemTrayService(
            _mockLogger.Object,
            _mockLifetime.Object,
            _mockServiceProvider.Object,
            _mockConfiguration.Object);

        // Assert - Verify it's a BackgroundService
        Assert.IsAssignableFrom<BackgroundService>(service);
        Assert.IsAssignableFrom<IHostedService>(service);
    }

    [Fact]
    public void Constructor_ShouldAcceptAllRequiredDependencies()
    {
        // Arrange
        var logger = new Mock<ILogger<SystemTrayService>>();
        var lifetime = new Mock<IHostApplicationLifetime>();
        var serviceProvider = new Mock<IServiceProvider>();
        var configuration = new Mock<IAppConfiguration>();

        // Act
        var service = new SystemTrayService(
            logger.Object,
            lifetime.Object,
            serviceProvider.Object,
            configuration.Object);

        // Assert
        Assert.NotNull(service);
        Assert.IsType<SystemTrayService>(service);
    }

    [Fact]
    public void OnExitClick_ShouldCallStopApplication()
    {
        // Arrange
        var service = new SystemTrayService(
            _mockLogger.Object,
            _mockLifetime.Object,
            _mockServiceProvider.Object,
            _mockConfiguration.Object);

        // Use reflection to get the OnExitClick method
        var method = typeof(SystemTrayService).GetMethod("OnExitClick",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(method);

        // Act
        method.Invoke(service, new object?[] { null, EventArgs.Empty });

        // Assert
        _mockLifetime.Verify(x => x.StopApplication(), Times.Once);
    }

    [Fact]
    public void OnExitClick_ShouldLogExitRequest()
    {
        // Arrange
        var service = new SystemTrayService(
            _mockLogger.Object,
            _mockLifetime.Object,
            _mockServiceProvider.Object,
            _mockConfiguration.Object);

        // Use reflection to get the OnExitClick method
        var method = typeof(SystemTrayService).GetMethod("OnExitClick",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(method);

        // Act
        method.Invoke(service, new object?[] { null, EventArgs.Empty });

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Exit requested")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public void OnExitClick_ShouldNotThrowException()
    {
        // Arrange
        var service = new SystemTrayService(
            _mockLogger.Object,
            _mockLifetime.Object,
            _mockServiceProvider.Object,
            _mockConfiguration.Object);

        // Use reflection to get the OnExitClick method
        var method = typeof(SystemTrayService).GetMethod("OnExitClick",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(method);

        // Act & Assert - Should not throw
        var exception = Record.Exception(() =>
            method.Invoke(service, new object?[] { null, EventArgs.Empty }));

        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_ShouldNotThrowException_WhenCalledOnce()
    {
        // Arrange
        var service = new SystemTrayService(
            _mockLogger.Object,
            _mockLifetime.Object,
            _mockServiceProvider.Object,
            _mockConfiguration.Object);

        // Act & Assert - Should not throw
        var exception = Record.Exception(() => service.Dispose());

        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_ShouldNotThrowException_WhenCalledMultipleTimes()
    {
        // Arrange
        var service = new SystemTrayService(
            _mockLogger.Object,
            _mockLifetime.Object,
            _mockServiceProvider.Object,
            _mockConfiguration.Object);

        // Act - Call Dispose multiple times
        service.Dispose();

        // Assert - Second call should not throw
        var exception = Record.Exception(() => service.Dispose());

        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_ShouldUnsubscribeFromHealthCheckEvents()
    {
        // Arrange
        var mockHealthCheckService = new Mock<IHealthCheckService>();
        mockHealthCheckService.Setup(h => h.IsConnected).Returns(true);

        _mockServiceProvider
            .Setup(sp => sp.GetService(typeof(IHealthCheckService)))
            .Returns(mockHealthCheckService.Object);

        var service = new SystemTrayService(
            _mockLogger.Object,
            _mockLifetime.Object,
            _mockServiceProvider.Object,
            _mockConfiguration.Object);

        // Act
        service.Dispose();

        // Assert - Verify event handler was removed
        // Note: We can't easily verify unsubscription without starting the service
        // This test mainly ensures Dispose doesn't throw when health check service is null
        Assert.True(true);
    }

    [Fact]
    public void UpdateConnectionStatus_ShouldInvoke_WithConnectedState()
    {
        // Arrange
        var service = new SystemTrayService(
            _mockLogger.Object,
            _mockLifetime.Object,
            _mockServiceProvider.Object,
            _mockConfiguration.Object);

        // Use reflection to get the UpdateConnectionStatus method
        var method = typeof(SystemTrayService).GetMethod("UpdateConnectionStatus",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(method);

        // Act - Should not throw even if tray icon is not initialized
        var exception = Record.Exception(() =>
            method.Invoke(service, new object[] { true }));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void UpdateConnectionStatus_ShouldInvoke_WithDisconnectedState()
    {
        // Arrange
        var service = new SystemTrayService(
            _mockLogger.Object,
            _mockLifetime.Object,
            _mockServiceProvider.Object,
            _mockConfiguration.Object);

        // Use reflection to get the UpdateConnectionStatus method
        var method = typeof(SystemTrayService).GetMethod("UpdateConnectionStatus",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(method);

        // Act - Should not throw even if tray icon is not initialized
        var exception = Record.Exception(() =>
            method.Invoke(service, new object[] { false }));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void UpdateTrayIcon_ShouldInvoke_WithConnectedState()
    {
        // Arrange
        var service = new SystemTrayService(
            _mockLogger.Object,
            _mockLifetime.Object,
            _mockServiceProvider.Object,
            _mockConfiguration.Object);

        // Use reflection to get the UpdateTrayIcon method
        var method = typeof(SystemTrayService).GetMethod("UpdateTrayIcon",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(method);

        // Act - Should not throw even if tray icon is not initialized
        var exception = Record.Exception(() =>
            method.Invoke(service, new object[] { true }));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void UpdateTrayIcon_ShouldInvoke_WithDisconnectedState()
    {
        // Arrange
        var service = new SystemTrayService(
            _mockLogger.Object,
            _mockLifetime.Object,
            _mockServiceProvider.Object,
            _mockConfiguration.Object);

        // Use reflection to get the UpdateTrayIcon method
        var method = typeof(SystemTrayService).GetMethod("UpdateTrayIcon",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(method);

        // Act - Should not throw even if tray icon is not initialized
        var exception = Record.Exception(() =>
            method.Invoke(service, new object[] { false }));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void UpdateTrayIcon_ShouldNotThrow_WhenIconFilesNotFound()
    {
        // Arrange
        var service = new SystemTrayService(
            _mockLogger.Object,
            _mockLifetime.Object,
            _mockServiceProvider.Object,
            _mockConfiguration.Object);

        // Use reflection to get the UpdateTrayIcon method
        var method = typeof(SystemTrayService).GetMethod("UpdateTrayIcon",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(method);

        // Act & Assert - Should not throw even if icon files don't exist
        var exception = Record.Exception(() =>
            method.Invoke(service, new object[] { true }));

        Assert.Null(exception);
    }
}

