using Microsoft.Win32;
using Xunit;

namespace HomeAssistantWindowsVolumeSync.Tests;

/// <summary>
/// Tests for WindowsStartupManager
/// </summary>
[Collection("WindowsStartup")]
public class WindowsStartupManagerTests : IDisposable
{
    private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "HomeAssistantWindowsVolumeSync";

    public WindowsStartupManagerTests()
    {
        // Clean up any existing test entries before each test
        CleanupStartupEntry();
    }

    [Fact]
    public void IsStartupEnabled_WhenNotRegistered_ReturnsFalse()
    {
        // Arrange
        CleanupStartupEntry();

        // Act
        var result = WindowsStartupManager.IsStartupEnabled();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void EnableStartup_CreatesRegistryEntry()
    {
        // Arrange
        CleanupStartupEntry();

        // Act
        WindowsStartupManager.EnableStartup();

        // Assert
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        var value = key?.GetValue(AppName) as string;
        Assert.NotNull(value);
        Assert.NotEmpty(value);
        Assert.Contains(".exe", value);

        // Cleanup
        CleanupStartupEntry();
    }

    [Fact]
    public void IsStartupEnabled_AfterEnable_ReturnsTrue()
    {
        // Arrange
        CleanupStartupEntry();

        // Act
        WindowsStartupManager.EnableStartup();
        var result = WindowsStartupManager.IsStartupEnabled();

        // Assert
        Assert.True(result);

        // Cleanup
        CleanupStartupEntry();
    }

    [Fact]
    public void DisableStartup_RemovesRegistryEntry()
    {
        // Arrange
        WindowsStartupManager.EnableStartup();
        Assert.True(WindowsStartupManager.IsStartupEnabled());

        // Act
        WindowsStartupManager.DisableStartup();

        // Assert
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        var value = key?.GetValue(AppName);
        Assert.Null(value);
    }

    [Fact]
    public void IsStartupEnabled_AfterDisable_ReturnsFalse()
    {
        // Arrange
        WindowsStartupManager.EnableStartup();
        Assert.True(WindowsStartupManager.IsStartupEnabled());

        // Act
        WindowsStartupManager.DisableStartup();
        var result = WindowsStartupManager.IsStartupEnabled();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void DisableStartup_WhenNotEnabled_DoesNotThrow()
    {
        // Arrange
        CleanupStartupEntry();

        // Act & Assert
        var exception = Record.Exception(() => WindowsStartupManager.DisableStartup());
        Assert.Null(exception);
    }

    [Fact]
    public void EnableStartup_MultipleTimes_DoesNotThrow()
    {
        // Arrange
        CleanupStartupEntry();

        // Act & Assert
        var exception = Record.Exception(() =>
        {
            WindowsStartupManager.EnableStartup();
            WindowsStartupManager.EnableStartup();
            WindowsStartupManager.EnableStartup();
        });
        Assert.Null(exception);

        // Verify still enabled
        Assert.True(WindowsStartupManager.IsStartupEnabled());

        // Cleanup
        CleanupStartupEntry();
    }

    [Fact]
    public void EnableStartup_SetsQuotedExecutablePath()
    {
        // Arrange
        CleanupStartupEntry();

        // Act
        WindowsStartupManager.EnableStartup();

        // Assert
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        var value = key?.GetValue(AppName) as string;
        Assert.NotNull(value);
        Assert.StartsWith("\"", value);
        Assert.EndsWith("\"", value);

        // Cleanup
        CleanupStartupEntry();
    }

    [Fact]
    public void DisableStartup_AfterEnable_CompletelyRemovesEntry()
    {
        // Arrange
        WindowsStartupManager.EnableStartup();

        // Act
        WindowsStartupManager.DisableStartup();

        // Assert
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        var value = key?.GetValue(AppName);
        Assert.Null(value);

        // Verify IsStartupEnabled reflects the change
        Assert.False(WindowsStartupManager.IsStartupEnabled());
    }

    private void CleanupStartupEntry()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (key?.GetValue(AppName) != null)
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    public void Dispose()
    {
        // Ensure cleanup after all tests
        CleanupStartupEntry();
    }
}
