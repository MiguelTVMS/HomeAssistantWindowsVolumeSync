using Microsoft.Win32;

namespace HomeAssistantWindowsVolumeSync;

/// <summary>
/// Manages Windows startup registry entries for the application
/// </summary>
public static class WindowsStartupManager
{
    private const string RunKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "HomeAssistantWindowsVolumeSync";

    /// <summary>
    /// Checks if the application is set to run at Windows startup
    /// </summary>
    public static bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            var value = key?.GetValue(AppName) as string;
            return !string.IsNullOrEmpty(value);
        }
        catch
        {
            // If we can't read the registry, assume it's not enabled
            return false;
        }
    }

    /// <summary>
    /// Enables the application to run at Windows startup
    /// </summary>
    public static void EnableStartup()
    {
        try
        {
            // Use CreateSubKey instead of OpenSubKey so the Run key is created if it doesn't
            // exist (e.g. on minimal Windows Server images or fresh CI runner environments).
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, true)
                ?? throw new InvalidOperationException(
                    $"Unable to create or open registry key: HKCU\\{RunKeyPath}");

            var exePath = Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
            key.SetValue(AppName, $"\"{exePath}\"");
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to enable startup: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Disables the application from running at Windows startup
    /// </summary>
    public static void DisableStartup()
    {
        try
        {
            // Use CreateSubKey so we never silently no-op if the key doesn't exist yet.
            using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, true)
                ?? throw new InvalidOperationException(
                    $"Unable to create or open registry key: HKCU\\{RunKeyPath}");

            if (key.GetValue(AppName) != null)
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to disable startup: {ex.Message}", ex);
        }
    }
}
