# HomeAssistantWindowsVolumeSync

HomeAssistantWindowsVolumeSync is a lightweight Windows service that synchronizes the **Windows system master volume** with a **Home Assistant** media player.
Whenever the volume changes in Windows, the service detects it and sends the updated value to a Home Assistant webhook in real time.

This allows Windows hardware volume keys, app volume sliders, and external volume knobs to directly control your media player volume through Home Assistant.

## Features

- **Event-driven volume detection** - no polling, near-zero CPU usage
- **Instant sync** - volume changes are sent immediately to Home Assistant
- **System tray integration** - convenient pause/resume controls from the taskbar
- **Native Windows Service** - runs in the background, starts automatically
- **Configurable** - easy webhook endpoint configuration
- **Universal compatibility** - works with any Home Assistant media player
- **Modern architecture** - built using .NET 8 Worker Service

## System Tray Icon

The service includes a system tray icon that appears in your Windows taskbar notification area, providing easy control over the volume sync:

- **Right-click** the icon to access the menu:
  - **Status** - Shows whether the service is running or paused
  - **Pause/Resume** - Temporarily pause or resume volume synchronization
  - **Exit** - Stop the service
- **Double-click** the icon to see a status dialog

When paused, the service continues to monitor Windows volume changes but does not send updates to Home Assistant. This is useful if you want to temporarily control your media player directly from Home Assistant without interference from Windows volume changes.

### Icon Creation

The project includes a Python script (`create_icon.py`) to generate the system tray icon based on the Material Design Icons `home-sound-out` icon:

```bash
python create_icon.py
```

This creates both `app.ico` and `app.png` files in the `src` directory. The PNG version is used by the application for the system tray icon.

## Prerequisites

### Home Assistant

- A working Home Assistant instance
- A media player entity (example: a speaker)
- Remote or LAN URL reachable from Windows

### Windows

- Windows 11 or later
- .NET 8 Runtime or newer (or use self-contained deployment)
- Administrator privileges for service installation

## Home Assistant Setup

You must create a Home Assistant automation that responds to a webhook and sets the media player volume.

### Steps

1. Create a webhook-based automation
2. Use the webhook ID: `homeassistant_windows_volume_sync`
3. Configure the automation to:
   - Read the JSON body `{ "volume": <number>, "mute": <boolean>, "target_media_player": <string> }`
   - Convert volume to 0.0–1.0 (the number is 0–100)
   - Use the `target_media_player` from the payload (or default to your media player)
   - Call `media_player.your_media_player`

After creating the automation, your webhook URL becomes:

```
https://<your-ha-url>/api/webhook/homeassistant_windows_volume_sync
```

Replace `<your-ha-url>` with your HA local URL or Nabu Casa remote address.

### Example Automation

An example automation YAML is provided in the `/HomeAssistant/automation.yaml` file.

## Project Structure

```
HomeAssistantWindowsVolumeSync/
├── src/
│   └── HomeAssistantWindowsVolumeSync/
│       ├── HomeAssistantWindowsVolumeSync.csproj
│       ├── Program.cs
│       ├── VolumeWatcherService.cs
│       ├── SystemTrayService.cs
│       ├── IHomeAssistantClient.cs
│       ├── HomeAssistantClient.cs
│       ├── appsettings.json
│       └── app.ico
├── tests/
│   └── HomeAssistantWindowsVolumeSync.Tests/
│       ├── HomeAssistantWindowsVolumeSync.Tests.csproj
│       └── HomeAssistantClientTests.cs
├── HomeAssistant/
│   └── automation.yaml
├── .vscode/
│   ├── launch.json
│   ├── tasks.json
│   ├── settings.json
│   └── extensions.json
├── copilot-instructions.md
├── README.md
└── LICENSE
```

## Configuration

The application provides two ways to configure settings:

### Configuration Availability

| Setting | Settings Window | appsettings.json | Description |
|---------|:---------------:|:----------------:|-------------|
| **Home Assistant URL** | ✓ | ✓ | Your Home Assistant base URL (e.g., `https://your-home-assistant-url`) |
| **Webhook ID** | ✓ | ✓ | The webhook identifier (default: `homeassistant_windows_volume_sync`) |
| **Target Media Player** | ✓ | ✓ | Your media player entity ID (e.g., `media_player.your_media_player`) |
| **Run on Startup** | ✓ | | Enable/disable automatic startup with Windows |
| **Webhook Path** | | ✓ | API webhook path (default: `/api/webhook/`) |
| **Strict TLS** | | ✓ | Enable/disable strict TLS certificate validation (default: `true`) |
| **Debounce Timer** | | ✓ | Time in milliseconds to wait after the last volume change before sending the update (default: `100`) |
| **Health Check Timer** | | ✓ | Interval in milliseconds between connection health checks (default: `5000` - 5 seconds) |
| **Health Check Retries** | | ✓ | Number of consecutive health check failures before marking as disconnected (default: `3`) |

**Settings Window** (right-click system tray icon → Settings):

- Provides a user-friendly interface for common settings
- Changes are saved automatically and applied immediately
- Best for general configuration

**appsettings.json** (manual editing):

- Allows access to all settings including advanced options
- Requires manual file editing
- Best for advanced configuration and fine-tuning

**Note:** Changes made through the Settings window are saved automatically and applied immediately.

### Configuration Example (appsettings.json)

```json
{
  "HomeAssistant": {
    "WebhookUrl": "https://your-home-assistant-url",
    "WebhookPath": "/api/webhook/",
    "WebhookId": "homeassistant_windows_volume_sync",
    "TargetMediaPlayer": "media_player.your_media_player",
    "StrictTLS": true,
    "DebounceTimer": 100,
    "HealthCheckTimer": 5000,
    "HealthCheckRetries": 3
  }
}
```

### Logging Configuration

The service supports comprehensive logging with different log levels and outputs. Logging is configured through `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "HomeAssistantWindowsVolumeSync": "Debug",
      "Microsoft": "Warning",
      "System.Net.Http.HttpClient": "Warning"
    },
    "Console": {
      "LogLevel": {
        "Default": "Debug"
      }
    },
    "EventLog": {
      "SourceName": "HomeAssistant Windows Volume Sync",
      "LogName": "Application",
      "LogLevel": {
        "Default": "Information"
      }
    }
  }
}
```

**Log Levels** (from most to least verbose):

- `Trace`: Extremely detailed diagnostic information
- `Debug`: Detailed information useful for debugging
- `Information`: General informational messages about application flow
- `Warning`: Potentially harmful situations or unexpected events
- `Error`: Error events that might still allow the application to continue
- `Critical`: Critical failures that require immediate attention

**Log Outputs:**

- **Console**: Logs appear in console window (when running in Debug mode or interactively)
- **Debug**: Logs appear in debugger output window during development
- **EventLog**: Logs written to Windows Event Viewer (Application log) when running as a service
- **Startup Error Log**: Fatal startup errors are written to `startup-error.log` in the application directory

**Recommended Settings:**

- **Production**: Use `Information` level to balance detail with performance
- **Development**: Use `Debug` or `Trace` level for detailed troubleshooting
- **Troubleshooting**: Temporarily set to `Debug` or `Trace`, then back to `Information` when resolved

**Viewing Logs:**

- Console logs: Visible when running in Debug mode or via `dotnet run`
- Event Viewer logs: Open Event Viewer → Windows Logs → Application → Filter by source "HomeAssistant Windows Volume Sync"
- Startup errors: Check `startup-error.log` file in the application directory if service fails to start

## Building the Service

### Build for Development

```bash
dotnet build src/HomeAssistantWindowsVolumeSync/HomeAssistantWindowsVolumeSync.csproj
```

### Build for Production

```bash
dotnet publish src/HomeAssistantWindowsVolumeSync -c Release -r win-x64 --self-contained false -o publish
```

This generates the service executable and required files in the `publish/` output folder.

## Installing the Service on Windows

1. Create a folder for the service:

```powershell
New-Item -ItemType Directory -Path "C:\Services\HomeAssistantWindowsVolumeSync" -Force
```

2. Copy the contents of the `publish` folder into this directory.

3. Update `appsettings.json` with your Home Assistant webhook URL.

4. Install the service:

```powershell
New-Service -Name "HomeAssistantWindowsVolumeSync" `
            -BinaryPathName "`"C:\Services\HomeAssistantWindowsVolumeSync\HomeAssistantWindowsVolumeSync.exe`"" `
            -DisplayName "HomeAssistant Windows Volume Sync" `
            -Description "Syncs Windows master volume to Home Assistant via webhook" `
            -StartupType Automatic
```

5. Start the service:

```powershell
Start-Service HomeAssistantWindowsVolumeSync
```

## Uninstalling the Service

1. Stop the service:

```powershell
Stop-Service HomeAssistantWindowsVolumeSync
```

2. Remove the service:

```powershell
Remove-Service HomeAssistantWindowsVolumeSync
```

If `Remove-Service` is unavailable in your PowerShell version, use:

```powershell
sc.exe delete HomeAssistantWindowsVolumeSync
```

## Running Tests

```bash
dotnet test tests/HomeAssistantWindowsVolumeSync.Tests/HomeAssistantWindowsVolumeSync.Tests.csproj
```

## Logging and Troubleshooting

- Logs appear in **Windows Event Viewer → Application Logs**
- Ensure the webhook URL in `appsettings.json` is correct
- Confirm the HA automation webhook ID matches
- Check firewall rules if using HTTPS internally
- The service logs at the following levels:
  - **Debug**: Volume change events
  - **Information**: Service lifecycle events
  - **Warning**: Configuration issues, failed HTTP requests
  - **Error**: Exceptions and critical failures

## Development

### Requirements

- .NET 8 SDK
- Visual Studio Code (recommended) or Visual Studio 2022

### VS Code Setup

1. Open the project folder in VS Code
2. Install recommended extensions when prompted
3. Use the provided tasks for building and testing

### Building and Debugging

Press `F5` in VS Code to build and run the service in debug mode.

## License

MIT License - see [LICENSE](LICENSE) for details.
