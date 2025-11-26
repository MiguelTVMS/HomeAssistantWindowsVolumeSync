# HomeAssistantWindowsVolumeSync

HomeAssistantWindowsVolumeSync is a lightweight Windows service that synchronizes the **Windows system master volume** with a **Home Assistant** media player (typically Sonos).
Whenever the volume changes in Windows, the service detects it and sends the updated value to a Home Assistant webhook in real time.

This allows Windows hardware volume keys, app volume sliders, and external volume knobs to directly control Sonos volume through Home Assistant.

## Features

- **Event-driven volume detection** - no polling, near-zero CPU usage
- **Instant sync** - volume changes are sent immediately to Home Assistant
- **Native Windows Service** - runs in the background, starts automatically
- **Configurable** - easy webhook endpoint configuration
- **Universal compatibility** - works with any Home Assistant media player, including Sonos
- **Modern architecture** - built using .NET 8 Worker Service

## Prerequisites

### Home Assistant

- A working Home Assistant instance
- A media player entity (example: a Sonos speaker)
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
   - Read the JSON body `{ "volume": <number>, "mute": <boolean> }`
   - Convert volume to 0.0–1.0 (the number is 0–100)
   - Call `media_player.volume_set`

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
│       ├── IHomeAssistantClient.cs
│       ├── HomeAssistantClient.cs
│       └── appsettings.json
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

Edit `appsettings.json` to configure the Home Assistant webhook URL:

```json
{
  "HomeAssistant": {
    "WebhookUrl": "https://your-home-assistant-url/api/webhook/homeassistant_windows_volume_sync"
  }
}
```

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
