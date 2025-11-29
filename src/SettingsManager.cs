using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace HomeAssistantWindowsVolumeSync;

/// <summary>
/// Service for managing application settings persistence
/// </summary>
public class SettingsManager
{
    private readonly string _settingsFilePath;
    private readonly IAppConfiguration? _appConfiguration;
    private readonly ILogger<SettingsManager>? _logger;

    public SettingsManager(IAppConfiguration? appConfiguration = null, ILogger<SettingsManager>? logger = null)
        : this(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), appConfiguration, logger)
    {
    }

    /// <summary>
    /// Constructor for testing purposes
    /// </summary>
    internal SettingsManager(string settingsFilePath, IAppConfiguration? appConfiguration = null, ILogger<SettingsManager>? logger = null)
    {
        _settingsFilePath = settingsFilePath;
        _appConfiguration = appConfiguration;
        _logger = logger;
    }

    /// <summary>
    /// Saves the Home Assistant settings to appsettings.json and reloads configuration
    /// </summary>
    public void SaveSettings(string webhookUrl, string webhookId, string targetMediaPlayer)
    {
        _logger?.LogInformation("Saving settings to {FilePath}", _settingsFilePath);
        _logger?.LogDebug("New settings - WebhookUrl: {Url}, WebhookId: {Id}, TargetMediaPlayer: {Player}",
            webhookUrl, webhookId, targetMediaPlayer);

        // Read the current settings file or create empty object if doesn't exist
        var json = File.Exists(_settingsFilePath) ? File.ReadAllText(_settingsFilePath) : "{}";
        using var jsonDoc = JsonDocument.Parse(json);

        // Create a mutable dictionary from the JSON
        var settings = new Dictionary<string, object?>();

        foreach (var property in jsonDoc.RootElement.EnumerateObject())
        {
            if (property.Name == "HomeAssistant")
            {
                // Update the HomeAssistant section while preserving other settings
                var homeAssistantSettings = new Dictionary<string, object?>();

                // Preserve existing settings
                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    foreach (var subProperty in property.Value.EnumerateObject())
                    {
                        homeAssistantSettings[subProperty.Name] = JsonSerializer.Deserialize<object>(subProperty.Value.GetRawText());
                    }
                }

                // Update the specific values
                homeAssistantSettings["WebhookUrl"] = webhookUrl;
                homeAssistantSettings["WebhookId"] = webhookId;
                homeAssistantSettings["TargetMediaPlayer"] = targetMediaPlayer;

                settings[property.Name] = homeAssistantSettings;
            }
            else
            {
                // Keep other sections as-is
                settings[property.Name] = JsonSerializer.Deserialize<object>(property.Value.GetRawText());
            }
        }

        // If HomeAssistant section doesn't exist, create it
        if (!settings.ContainsKey("HomeAssistant"))
        {
            settings["HomeAssistant"] = new Dictionary<string, object?>
            {
                ["WebhookUrl"] = webhookUrl,
                ["WebhookPath"] = "/api/webhook/",
                ["WebhookId"] = webhookId,
                ["TargetMediaPlayer"] = targetMediaPlayer,
                ["StrictTLS"] = true  // Default to secure
            };
        }

        // Write the updated settings back to the file
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var updatedJson = JsonSerializer.Serialize(settings, options);
        File.WriteAllText(_settingsFilePath, updatedJson);

        _logger?.LogInformation("Settings file written successfully");

        // Reload configuration to apply changes immediately
        if (_appConfiguration != null)
        {
            _logger?.LogInformation("Reloading configuration...");

            // Wait for configuration to reload with new values (retry up to 2 seconds)
            _appConfiguration.Reload();

            const int maxAttempts = 20;
            const int delayMs = 100;
            int attempt = 0;
            bool configUpdated = false;
            while (attempt < maxAttempts)
            {
                if (string.Equals(_appConfiguration.WebhookUrl, webhookUrl, StringComparison.Ordinal) &&
                    string.Equals(_appConfiguration.WebhookId, webhookId, StringComparison.Ordinal) &&
                    string.Equals(_appConfiguration.TargetMediaPlayer, targetMediaPlayer, StringComparison.Ordinal))
                {
                    configUpdated = true;
                    break;
                }
                attempt++;
                System.Threading.Tasks.Task.Delay(delayMs).Wait();
                _appConfiguration.Reload();
            }

            if (configUpdated)
            {
                _logger?.LogInformation("Configuration reloaded. New values - WebhookUrl: {Url}, WebhookId: {Id}, TargetMediaPlayer: {Player}",
                    _appConfiguration.WebhookUrl, _appConfiguration.WebhookId, _appConfiguration.TargetMediaPlayer);
            }
            else
            {
                _logger?.LogWarning("Configuration reload timed out. Values may not be updated yet. WebhookUrl: {Url}, WebhookId: {Id}, TargetMediaPlayer: {Player}",
                    _appConfiguration.WebhookUrl, _appConfiguration.WebhookId, _appConfiguration.TargetMediaPlayer);
            }
        }
        else
        {
            _logger?.LogWarning("No app configuration provided, settings will not be reloaded automatically");
        }
    }
}
