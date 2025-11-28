using System.Linq;
using Xunit;

namespace HomeAssistantWindowsVolumeSync.Tests;

/// <summary>
/// Tests for IHealthCheckService interface contract.
/// These tests document the expected behavior of any implementation.
/// </summary>
public class IHealthCheckServiceTests
{
    [Fact]
    public void Interface_ShouldBePublic()
    {
        // Arrange
        var interfaceType = typeof(IHealthCheckService);

        // Assert
        Assert.True(interfaceType.IsPublic);
        Assert.True(interfaceType.IsInterface);
    }

    [Fact]
    public void Interface_ShouldHaveCorrectNamespace()
    {
        // Arrange
        var interfaceType = typeof(IHealthCheckService);

        // Assert
        Assert.Equal("HomeAssistantWindowsVolumeSync", interfaceType.Namespace);
    }

    [Fact]
    public void Interface_ShouldHaveConnectionStateChangedEvent()
    {
        // Arrange
        var interfaceType = typeof(IHealthCheckService);

        // Act
        var eventInfo = interfaceType.GetEvent("ConnectionStateChanged");

        // Assert
        Assert.NotNull(eventInfo);
        Assert.Equal(typeof(EventHandler<bool>), eventInfo.EventHandlerType);
    }

    [Fact]
    public void Interface_ShouldHaveIsConnectedProperty()
    {
        // Arrange
        var interfaceType = typeof(IHealthCheckService);

        // Act
        var property = interfaceType.GetProperty("IsConnected");

        // Assert
        Assert.NotNull(property);
        Assert.Equal(typeof(bool), property.PropertyType);
        Assert.True(property.CanRead);
        Assert.False(property.CanWrite);
    }

    [Fact]
    public void Interface_ShouldHaveConsecutiveFailuresProperty()
    {
        // Arrange
        var interfaceType = typeof(IHealthCheckService);

        // Act
        var property = interfaceType.GetProperty("ConsecutiveFailures");

        // Assert
        Assert.NotNull(property);
        Assert.Equal(typeof(int), property.PropertyType);
        Assert.True(property.CanRead);
        Assert.False(property.CanWrite);
    }

    [Fact]
    public void Interface_ShouldHaveStartAsyncMethod()
    {
        // Arrange
        var interfaceType = typeof(IHealthCheckService);

        // Act
        var method = interfaceType.GetMethod("StartAsync");

        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method.ReturnType);

        var parameters = method.GetParameters();
        Assert.Single(parameters);
        Assert.Equal("cancellationToken", parameters[0].Name);
        Assert.Equal(typeof(CancellationToken), parameters[0].ParameterType);
    }

    [Fact]
    public void Interface_ShouldHaveStopAsyncMethod()
    {
        // Arrange
        var interfaceType = typeof(IHealthCheckService);

        // Act
        var method = interfaceType.GetMethod("StopAsync");

        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method.ReturnType);

        var parameters = method.GetParameters();
        Assert.Single(parameters);
        Assert.Equal("cancellationToken", parameters[0].Name);
        Assert.Equal(typeof(CancellationToken), parameters[0].ParameterType);
    }

    [Fact]
    public void Interface_ShouldHaveCheckHealthAsyncMethod()
    {
        // Arrange
        var interfaceType = typeof(IHealthCheckService);

        // Act
        var method = interfaceType.GetMethod("CheckHealthAsync");

        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(Task<bool>), method.ReturnType);
        Assert.Empty(method.GetParameters());
    }

    [Fact]
    public void HealthCheckService_ShouldImplementInterface()
    {
        // Arrange
        var serviceType = typeof(HealthCheckService);

        // Assert
        Assert.True(typeof(IHealthCheckService).IsAssignableFrom(serviceType));
    }

    [Fact]
    public void Interface_ShouldHaveExpectedMemberCount()
    {
        // Arrange
        var interfaceType = typeof(IHealthCheckService);

        // Act
        var properties = interfaceType.GetProperties();
        var methods = interfaceType.GetMethods().Where(m => !m.IsSpecialName).ToArray();
        var events = interfaceType.GetEvents();

        // Assert
        Assert.Equal(2, properties.Length); // IsConnected, ConsecutiveFailures
        Assert.Equal(3, methods.Length); // StartAsync, StopAsync, CheckHealthAsync
        Assert.Single(events); // ConnectionStateChanged
    }
}
