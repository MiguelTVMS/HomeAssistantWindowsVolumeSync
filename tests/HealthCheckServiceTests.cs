using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HomeAssistantWindowsVolumeSync.Tests;

public class HealthCheckServiceTests
{
    private readonly Mock<ILogger<HealthCheckService>> _mockLogger;
    private readonly Mock<IHomeAssistantClient> _mockHomeAssistantClient;
    private readonly Mock<IAppConfiguration> _mockConfiguration;

    public HealthCheckServiceTests()
    {
        _mockLogger = new Mock<ILogger<HealthCheckService>>();
        _mockHomeAssistantClient = new Mock<IHomeAssistantClient>();
        _mockConfiguration = new Mock<IAppConfiguration>();

        _mockConfiguration.Setup(c => c.HealthCheckTimer).Returns(5000);
        _mockConfiguration.Setup(c => c.HealthCheckRetries).Returns(3);
    }

    [Fact]
    public void Constructor_ShouldInitialize_Successfully()
    {
        // Act
        var service = new HealthCheckService(
            _mockLogger.Object,
            _mockHomeAssistantClient.Object,
            _mockConfiguration.Object);

        // Assert
        Assert.NotNull(service);
        Assert.True(service.IsConnected); // Starts as connected
        Assert.Equal(0, service.ConsecutiveFailures);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenSuccessful_ShouldReturnTrue()
    {
        // Arrange
        _mockHomeAssistantClient.Setup(c => c.CheckHealthAsync())
            .ReturnsAsync(true);

        var service = new HealthCheckService(
            _mockLogger.Object,
            _mockHomeAssistantClient.Object,
            _mockConfiguration.Object);

        // Act
        var result = await service.CheckHealthAsync();

        // Assert
        Assert.True(result);
        Assert.True(service.IsConnected);
        Assert.Equal(0, service.ConsecutiveFailures);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenFails_ShouldIncrementFailureCount()
    {
        // Arrange
        _mockHomeAssistantClient.Setup(c => c.CheckHealthAsync())
            .ReturnsAsync(false);

        var service = new HealthCheckService(
            _mockLogger.Object,
            _mockHomeAssistantClient.Object,
            _mockConfiguration.Object);

        // Act
        var result = await service.CheckHealthAsync();

        // Assert
        Assert.False(result);
        Assert.Equal(1, service.ConsecutiveFailures);
    }

    [Fact]
    public async Task CheckHealthAsync_AfterThreeFailures_ShouldMarkAsDisconnected()
    {
        // Arrange
        _mockHomeAssistantClient.Setup(c => c.CheckHealthAsync())
            .ReturnsAsync(false);

        var service = new HealthCheckService(
            _mockLogger.Object,
            _mockHomeAssistantClient.Object,
            _mockConfiguration.Object);

        // Act
        await service.CheckHealthAsync(); // Failure 1
        Assert.True(service.IsConnected);
        Assert.Equal(1, service.ConsecutiveFailures);

        await service.CheckHealthAsync(); // Failure 2
        Assert.True(service.IsConnected);
        Assert.Equal(2, service.ConsecutiveFailures);

        await service.CheckHealthAsync(); // Failure 3
        Assert.False(service.IsConnected);
        Assert.Equal(3, service.ConsecutiveFailures);
    }

    [Fact]
    public async Task CheckHealthAsync_AfterReconnect_ShouldResetFailureCount()
    {
        // Arrange
        var service = new HealthCheckService(
            _mockLogger.Object,
            _mockHomeAssistantClient.Object,
            _mockConfiguration.Object);

        // Simulate failures
        _mockHomeAssistantClient.Setup(c => c.CheckHealthAsync())
            .ReturnsAsync(false);

        await service.CheckHealthAsync();
        await service.CheckHealthAsync();
        Assert.Equal(2, service.ConsecutiveFailures);

        // Simulate success
        _mockHomeAssistantClient.Setup(c => c.CheckHealthAsync())
            .ReturnsAsync(true);

        // Act
        await service.CheckHealthAsync();

        // Assert
        Assert.True(service.IsConnected);
        Assert.Equal(0, service.ConsecutiveFailures);
    }

    [Fact]
    public async Task ConnectionStateChanged_ShouldRaiseEvent_WhenStateChanges()
    {
        // Arrange
        _mockHomeAssistantClient.Setup(c => c.CheckHealthAsync())
            .ReturnsAsync(false);

        var service = new HealthCheckService(
            _mockLogger.Object,
            _mockHomeAssistantClient.Object,
            _mockConfiguration.Object);

        bool eventRaised = false;
        bool eventIsConnected = true;

        service.ConnectionStateChanged += (sender, isConnected) =>
        {
            eventRaised = true;
            eventIsConnected = isConnected;
        };

        // Act - Cause 3 failures to trigger disconnection
        await service.CheckHealthAsync();
        await service.CheckHealthAsync();
        await service.CheckHealthAsync();

        // Assert
        Assert.True(eventRaised);
        Assert.False(eventIsConnected);
    }

    [Fact]
    public async Task StartAsync_ShouldPerformInitialHealthCheck()
    {
        // Arrange
        _mockHomeAssistantClient.Setup(c => c.CheckHealthAsync())
            .ReturnsAsync(true);

        var service = new HealthCheckService(
            _mockLogger.Object,
            _mockHomeAssistantClient.Object,
            _mockConfiguration.Object);

        // Act
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(100); // Give time for async health check

        // Assert
        _mockHomeAssistantClient.Verify(c => c.CheckHealthAsync(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task StopAsync_ShouldComplete_WithoutErrors()
    {
        // Arrange
        var service = new HealthCheckService(
            _mockLogger.Object,
            _mockHomeAssistantClient.Object,
            _mockConfiguration.Object);

        await service.StartAsync(CancellationToken.None);

        // Act & Assert
        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Arrange
        var service = new HealthCheckService(
            _mockLogger.Object,
            _mockHomeAssistantClient.Object,
            _mockConfiguration.Object);

        // Act & Assert
        service.Dispose();
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldUseConfiguredRetries()
    {
        // Arrange - Set retries to 2 instead of default 3
        _mockConfiguration.Setup(c => c.HealthCheckRetries).Returns(2);
        _mockHomeAssistantClient.Setup(c => c.CheckHealthAsync())
            .ReturnsAsync(false);

        var service = new HealthCheckService(
            _mockLogger.Object,
            _mockHomeAssistantClient.Object,
            _mockConfiguration.Object);

        bool disconnectedEventFired = false;
        service.ConnectionStateChanged += (sender, isConnected) =>
        {
            if (!isConnected) disconnectedEventFired = true;
        };

        // Act - First failure
        await service.CheckHealthAsync();
        Assert.False(disconnectedEventFired); // Not disconnected yet
        Assert.Equal(1, service.ConsecutiveFailures);

        // Act - Second failure should trigger disconnection with retries=2
        await service.CheckHealthAsync();

        // Assert
        Assert.True(disconnectedEventFired);
        Assert.False(service.IsConnected);
        Assert.Equal(2, service.ConsecutiveFailures);
    }
}
