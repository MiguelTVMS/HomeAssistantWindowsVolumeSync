using Xunit;

namespace HomeAssistantWindowsVolumeSync.Tests;

/// <summary>
/// Tests for WindowsStartupManager static class interface and contract
/// </summary>
public class IWindowsStartupManagerTests
{
    [Fact]
    public void WindowsStartupManager_HasIsStartupEnabledMethod()
    {
        // Arrange & Act
        var methodInfo = typeof(WindowsStartupManager).GetMethod(
            nameof(WindowsStartupManager.IsStartupEnabled),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Assert
        Assert.NotNull(methodInfo);
        Assert.Equal(typeof(bool), methodInfo.ReturnType);
        Assert.Empty(methodInfo.GetParameters());
    }

    [Fact]
    public void WindowsStartupManager_HasEnableStartupMethod()
    {
        // Arrange & Act
        var methodInfo = typeof(WindowsStartupManager).GetMethod(
            nameof(WindowsStartupManager.EnableStartup),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Assert
        Assert.NotNull(methodInfo);
        Assert.Equal(typeof(void), methodInfo.ReturnType);
        Assert.Empty(methodInfo.GetParameters());
    }

    [Fact]
    public void WindowsStartupManager_HasDisableStartupMethod()
    {
        // Arrange & Act
        var methodInfo = typeof(WindowsStartupManager).GetMethod(
            nameof(WindowsStartupManager.DisableStartup),
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        // Assert
        Assert.NotNull(methodInfo);
        Assert.Equal(typeof(void), methodInfo.ReturnType);
        Assert.Empty(methodInfo.GetParameters());
    }

    [Fact]
    public void WindowsStartupManager_IsStaticClass()
    {
        // Arrange & Act
        var type = typeof(WindowsStartupManager);

        // Assert
        Assert.True(type.IsAbstract && type.IsSealed, "WindowsStartupManager should be a static class");
    }

    [Fact]
    public void WindowsStartupManager_HasExpectedPublicMethods()
    {
        // Arrange
        var type = typeof(WindowsStartupManager);

        // Act
        var methods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly);

        // Assert
        Assert.Equal(3, methods.Length);
        Assert.Contains(methods, m => m.Name == nameof(WindowsStartupManager.IsStartupEnabled));
        Assert.Contains(methods, m => m.Name == nameof(WindowsStartupManager.EnableStartup));
        Assert.Contains(methods, m => m.Name == nameof(WindowsStartupManager.DisableStartup));
    }

    [Fact]
    public void IsStartupEnabled_CanBeCalledWithoutException()
    {
        // Act & Assert
        var exception = Record.Exception(() => WindowsStartupManager.IsStartupEnabled());
        Assert.Null(exception);
    }

    [Fact]
    public void EnableStartup_CanBeCalledWithoutException()
    {
        // Act & Assert - Try to enable (may fail in test environment but shouldn't throw in normal cases)
        try
        {
            WindowsStartupManager.EnableStartup();
            // Cleanup if successful
            WindowsStartupManager.DisableStartup();
        }
        catch (InvalidOperationException)
        {
            // Expected in some environments
        }

        // Should not throw unexpected exceptions
        Assert.True(true);
    }

    [Fact]
    public void DisableStartup_CanBeCalledWithoutException()
    {
        // Act & Assert
        var exception = Record.Exception(() => WindowsStartupManager.DisableStartup());

        // DisableStartup should never throw, even if entry doesn't exist
        Assert.Null(exception);
    }

    [Fact]
    public void WindowsStartupManager_MethodsAreConsistent()
    {
        // Arrange - Clean state
        WindowsStartupManager.DisableStartup();

        try
        {
            // Verify initial state is disabled
            Assert.False(WindowsStartupManager.IsStartupEnabled());

            // Act - Enable and check
            WindowsStartupManager.EnableStartup();
            var enabledState = WindowsStartupManager.IsStartupEnabled();

            // Assert - Should be enabled
            Assert.True(enabledState);

            // Act - Disable and check
            WindowsStartupManager.DisableStartup();
            var disabledState = WindowsStartupManager.IsStartupEnabled();

            // Assert - Should be disabled
            Assert.False(disabledState);
        }
        finally
        {
            // Cleanup - Ensure disabled
            WindowsStartupManager.DisableStartup();
        }
    }
}
