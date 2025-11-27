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
    private VolumeWatcherService? _volumeWatcherService;

    public SystemTrayServiceTests()
    {
        _mockLogger = new Mock<ILogger<SystemTrayService>>();
        _mockLifetime = new Mock<IHostApplicationLifetime>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockServiceScope = new Mock<IServiceScope>();
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();

        // Create a real VolumeWatcherService with mocked dependencies
        var mockVolumeWatcherLogger = new Mock<ILogger<VolumeWatcherService>>();
        var mockHomeAssistantClient = new Mock<IHomeAssistantClient>();
        var mockConfiguration = new Mock<IConfiguration>();

        _volumeWatcherService = new VolumeWatcherService(
            mockVolumeWatcherLogger.Object,
            mockHomeAssistantClient.Object,
            mockConfiguration.Object);

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
            _mockServiceProvider.Object);

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
            _mockServiceProvider.Object);

        Assert.NotNull(service);
    }

    [Fact]
    public void Dispose_ShouldDisposeResourcesSafely()
    {
        // Arrange
        var service = new SystemTrayService(
            _mockLogger.Object,
            _mockLifetime.Object,
            _mockServiceProvider.Object);

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
            _mockServiceProvider.Object);

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

        // Act
        var service = new SystemTrayService(
            logger.Object,
            lifetime.Object,
            serviceProvider.Object);

        // Assert
        Assert.NotNull(service);
        Assert.IsType<SystemTrayService>(service);
    }
}
