using Xunit;

namespace HomeAssistantWindowsVolumeSync.Tests;

/// <summary>
/// Tests for IHomeAssistantClient interface contract.
/// These tests document the expected behavior of any implementation.
/// </summary>
public class IHomeAssistantClientTests
{
    [Fact]
    public void Interface_ShouldHaveSendVolumeUpdateAsyncMethod()
    {
        // Arrange
        var interfaceType = typeof(IHomeAssistantClient);

        // Act
        var method = interfaceType.GetMethod("SendVolumeUpdateAsync");

        // Assert
        Assert.NotNull(method);
        Assert.Equal(typeof(Task), method.ReturnType);

        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal("volumePercent", parameters[0].Name);
        Assert.Equal(typeof(int), parameters[0].ParameterType);
        Assert.Equal("isMuted", parameters[1].Name);
        Assert.Equal(typeof(bool), parameters[1].ParameterType);
    }

    [Fact]
    public void Interface_ShouldBePublic()
    {
        // Arrange
        var interfaceType = typeof(IHomeAssistantClient);

        // Assert
        Assert.True(interfaceType.IsPublic);
        Assert.True(interfaceType.IsInterface);
    }

    [Fact]
    public void Interface_ShouldHaveCorrectNamespace()
    {
        // Arrange
        var interfaceType = typeof(IHomeAssistantClient);

        // Assert
        Assert.Equal("HomeAssistantWindowsVolumeSync", interfaceType.Namespace);
    }

    [Fact]
    public void HomeAssistantClient_ShouldImplementInterface()
    {
        // Arrange
        var clientType = typeof(HomeAssistantClient);

        // Assert
        Assert.True(typeof(IHomeAssistantClient).IsAssignableFrom(clientType));
    }
}
