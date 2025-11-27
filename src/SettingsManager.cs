using System.Text.Json;

namespace HomeAssistantWindowsVolumeSync;

/// <summary>
/// Service for managing application settings persistence
/// </summary>
public class SettingsManager
{
    private readonly string _settingsFilePath;

    public SettingsManager()
    {
        _settingsFilePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    }

    /// <summary>
    /// Saves the Home Assistant settings to appsettings.json
    /// </summary>
    public void SaveSettings(string webhookUrl, string targetMediaPlayer)
    {
        // Read the current settings file
        var json = File.ReadAllText(_settingsFilePath);
        var jsonDoc = JsonDocument.Parse(json);

        // Create a mutable dictionary from the JSON
        var settings = new Dictionary<string, object?>();

        foreach (var property in jsonDoc.RootElement.EnumerateObject())
        {
            if (property.Name == "HomeAssistant")
            {
                // Update the HomeAssistant section
                settings[property.Name] = new Dictionary<string, string>
                {
                    ["WebhookUrl"] = webhookUrl,
                    ["TargetMediaPlayer"] = targetMediaPlayer
                };
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
            settings["HomeAssistant"] = new Dictionary<string, string>
            {
                ["WebhookUrl"] = webhookUrl,
                ["TargetMediaPlayer"] = targetMediaPlayer
            };
        }

        // Write the updated settings back to the file
        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var updatedJson = JsonSerializer.Serialize(settings, options);
        File.WriteAllText(_settingsFilePath, updatedJson);
    }
}
