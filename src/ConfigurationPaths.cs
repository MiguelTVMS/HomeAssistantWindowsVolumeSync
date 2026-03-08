namespace HomeAssistantWindowsVolumeSync;

/// <summary>
/// Resolves canonical paths for application configuration files.
/// User-editable config is stored in the roaming application data folder (%APPDATA%)
/// so the app functions correctly regardless of where it is installed (for example,
/// in Program Files, a per-user folder under %LOCALAPPDATA%, or a dev build folder).
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
    /// Safe to call concurrently — treats "file already exists" as success.
    /// </summary>
    public static void EnsureUserConfigExists() =>
        EnsureUserConfigExists(GetUserConfigDirectory(), GetDefaultConfigFilePath());

    /// <summary>
    /// Testable overload: same logic as <see cref="EnsureUserConfigExists()"/> but
    /// operates on caller-supplied paths instead of the production %APPDATA% locations.
    /// </summary>
    internal static void EnsureUserConfigExists(string userConfigDirectory, string defaultConfigFilePath)
    {
        Directory.CreateDirectory(userConfigDirectory);

        var userConfigFile = Path.Combine(userConfigDirectory, SettingsFileName);

        if (File.Exists(userConfigFile))
            return;

        try
        {
            if (File.Exists(defaultConfigFilePath))
            {
                File.Copy(defaultConfigFilePath, userConfigFile);
            }
            else
            {
                // No default to seed from — write a minimal valid config
                File.WriteAllText(userConfigFile, "{}");
            }
        }
        catch (IOException)
        {
            // Another process created the file between our File.Exists check and the
            // copy/write — treat this as success (idempotent startup).
            if (!File.Exists(userConfigFile))
                throw;
        }
    }
}
