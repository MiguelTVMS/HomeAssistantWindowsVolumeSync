namespace HomeAssistantWindowsVolumeSync;

/// <summary>
/// Resolves canonical paths for application configuration files.
/// User-editable config is stored in %APPDATA% so the app functions correctly
/// whether installed to a protected location (Program Files) or a user-writable
/// location (%LOCALAPPDATA%, dev build folder, etc.).
/// </summary>
public static class ConfigurationPaths
{
    private const string AppFolderName = "HomeAssistantWindowsVolumeSync";
    private const string SettingsFileName = "appsettings.json";

    /// <summary>
    /// Returns the per-user configuration directory:
    /// %APPDATA%\HomeAssistantWindowsVolumeSync\
    /// </summary>
    public static string GetUserConfigDirectory() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppFolderName);

    /// <summary>
    /// Returns the full path to the user's appsettings.json:
    /// %APPDATA%\HomeAssistantWindowsVolumeSync\appsettings.json
    /// </summary>
    public static string GetUserConfigFilePath() =>
        Path.Combine(GetUserConfigDirectory(), SettingsFileName);

    /// <summary>
    /// Returns the path to the default (shipped) appsettings.json that lives
    /// next to the executable. Used as a seed template on first run.
    /// </summary>
    public static string GetDefaultConfigFilePath() =>
        Path.Combine(AppContext.BaseDirectory, SettingsFileName);

    /// <summary>
    /// Ensures the user config directory exists and seeds it with the default
    /// appsettings.json if no user config file exists yet (first run).
    /// </summary>
    public static void EnsureUserConfigExists()
    {
        var userConfigDir = GetUserConfigDirectory();
        var userConfigFile = GetUserConfigFilePath();

        Directory.CreateDirectory(userConfigDir);

        if (!File.Exists(userConfigFile))
        {
            var defaultConfigFile = GetDefaultConfigFilePath();
            if (File.Exists(defaultConfigFile))
            {
                File.Copy(defaultConfigFile, userConfigFile);
            }
            else
            {
                // No default to seed from — write a minimal valid config
                File.WriteAllText(userConfigFile, "{}");
            }
        }
    }
}
