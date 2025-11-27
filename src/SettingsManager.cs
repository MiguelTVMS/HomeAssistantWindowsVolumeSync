using System.Text.Json;

namespace HomeAssistantWindowsVolumeSync;

/// <summary>
/// Service for managing application settings persistence
/// </summary>
public class SettingsManager
{
    private readonly string _settingsFilePath;
    private readonly IAppConfiguration? _appConfiguration;

    public SettingsManager(IAppConfiguration? appConfiguration = null)
        : this(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), appConfiguration)
    {
    }

    /// <summary>
    /// Constructor for testing purposes
    /// </summary>
    internal SettingsManager(string settingsFilePath, IAppConfiguration? appConfiguration = null)
    {
        _settingsFilePath = settingsFilePath;
        _appConfiguration = appConfiguration;
    }

    /// <summary>
    /// Saves the Home Assistant settings to appsettings.json and reloads configuration
    /// </summary>
    public void SaveSettings(string webhookUrl, string webhookId, string targetMediaPlayer)
    {
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

        // Reload configuration to apply changes immediately
        if (_appConfiguration != null)
        {
            // Small delay to ensure file write is complete
            Thread.Sleep(100);
            _appConfiguration.Reload();
        }
    }
}
