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
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (key != null)
            {
                var exePath = Environment.ProcessPath ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
                key.SetValue(AppName, $"\"{exePath}\"");
            }
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
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (key != null)
            {
                // Only try to delete if the value exists
                if (key.GetValue(AppName) != null)
                {
                    key.DeleteValue(AppName, false);
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to disable startup: {ex.Message}", ex);
        }
    }
}
