# GitHub Copilot Instructions for HomeAssistantWindowsVolumeSync

## Project Overview

HomeAssistantWindowsVolumeSync is a Windows service that synchronizes the Windows system master volume with Home Assistant via webhooks. It uses NAudio to subscribe to volume change events and sends updates in real-time.

## Technology Stack

- **Framework**: .NET 8.0 (Windows)
- **Service Type**: Windows Service using `Microsoft.Extensions.Hosting.WindowsServices`
- **Audio Library**: NAudio for Windows Core Audio API access
- **HTTP Client**: `System.Net.Http` with `IHttpClientFactory`
- **Testing**: xUnit with Moq for mocking

## Coding Conventions

### General Guidelines

1. Use C# 12 features and .NET 8 APIs
2. Enable nullable reference types (`<Nullable>enable</Nullable>`)
3. Use implicit usings (`<ImplicitUsings>enable</ImplicitUsings>`)
4. Follow Microsoft C# coding conventions

### Naming Conventions

- Use PascalCase for public members, types, and namespaces
- Use camelCase with underscore prefix for private fields (`_logger`)
- Use PascalCase for async methods with `Async` suffix
- Interface names start with `I` (`IHomeAssistantClient`)

### Dependency Injection

- Register services in `Program.cs`
- Use constructor injection
- Prefer interfaces over concrete implementations
- Use `IHttpClientFactory` for HTTP clients

### Logging

- Use `ILogger<T>` for logging
- Log at appropriate levels:
  - `LogDebug`: Detailed debugging information
  - `LogInformation`: General operational messages
  - `LogWarning`: Recoverable issues
  - `LogError`: Errors that should be investigated

### Error Handling

- Use structured exception handling
- Log exceptions with context
- Don't swallow exceptions silently
- Use appropriate exception types

### Testing

- Use xUnit for unit tests
- Use Moq for mocking dependencies
- Test both success and failure scenarios
- Use descriptive test names following the pattern: `MethodName_Scenario_ExpectedResult`

## File Structure

```
src/
  HomeAssistantWindowsVolumeSync/
    HomeAssistantWindowsVolumeSync.csproj  # Project file
    Program.cs                              # Service configuration
    VolumeWatcherService.cs                 # Volume monitoring service
    IHomeAssistantClient.cs                 # Home Assistant client interface
    HomeAssistantClient.cs                  # Home Assistant client implementation
    appsettings.json                        # Configuration

tests/
  HomeAssistantWindowsVolumeSync.Tests/
    HomeAssistantWindowsVolumeSync.Tests.csproj  # Test project
    HomeAssistantClientTests.cs                   # Client tests

HomeAssistant/
  automation.yaml                           # HA automation example
```

## Configuration

The service uses `appsettings.json` for configuration:

```json
{
  "HomeAssistant": {
    "WebhookUrl": "https://your-ha-url/api/webhook/homeassistant_windows_volume_sync"
  }
}
```

## Building and Running

```bash
# Build
dotnet build src/HomeAssistantWindowsVolumeSync.csproj

# Run tests
dotnet test tests/HomeAssistantWindowsVolumeSync.Tests.csproj

# Publish for production
dotnet publish src -c Release -r win-x64 --self-contained false -o publish
```

## Key Implementation Details

### Volume Monitoring

- Uses `NAudio.CoreAudioApi.MMDeviceEnumerator` to access audio devices
- Subscribes to `AudioEndpointVolume.OnVolumeNotification` for event-driven updates
- Converts volume from 0.0-1.0 scalar to 0-100 percentage

### Webhook Payload

The service sends JSON payloads to Home Assistant:

```json
{
  "volume": 50,
  "mute": false
}
```

### Service Lifecycle

1. Service starts and initializes audio device monitoring
2. Initial volume state is sent to Home Assistant
3. Volume changes trigger webhook calls
4. Service gracefully handles cancellation and disposes resources

## When Making Changes

1. Update tests for any new functionality
2. Keep the Home Assistant automation example in sync with payload format
3. Document any new configuration options
4. Follow existing patterns for dependency injection and logging
