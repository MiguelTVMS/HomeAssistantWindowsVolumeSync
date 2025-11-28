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
- **ALWAYS create tests for new functionality**
- **ALWAYS run tests after making changes**
- Maintain test coverage for all public APIs
- Test edge cases and error conditions

## Test-Driven Development Requirements

### After Every Implementation:

1. **Write Tests First** (when possible):

   - Write tests before implementing new features
   - Define expected behavior through tests
   - Use tests to drive API design

2. **Build and Test Verification** (MANDATORY):

   - Run `dotnet build HomeAssistantWindowsVolumeSync.sln` after changes
   - Run `dotnet test HomeAssistantWindowsVolumeSync.sln` to verify all tests pass
   - Fix any compilation errors or test failures immediately
   - Verify both Debug and Release configurations build successfully

3. **Test Coverage Requirements**:

   - Every new class must have corresponding test file
   - Every public method should have at least one test
   - Test success paths and error/exception paths
   - Use Theory tests for multiple input scenarios

4. **Before Committing**:
   - Ensure all tests pass (34+ tests currently)
   - No build warnings or errors
   - Code follows project conventions
   - Tests follow naming conventions

### Test File Organization

Each source file should have a corresponding test file:

- `HomeAssistantClient.cs` → `HomeAssistantClientTests.cs`
- `VolumeWatcherService.cs` → `VolumeWatcherServiceTests.cs`
- `SystemTrayService.cs` → `SystemTrayServiceTests.cs`
- Interfaces get contract tests: `IHomeAssistantClient.cs` → `IHomeAssistantClientTests.cs`

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
    IHomeAssistantClientTests.cs                  # Interface contract tests
    VolumeWatcherServiceTests.cs                  # Volume watcher tests
    SystemTrayServiceTests.cs                     # System tray tests

HomeAssistant/
  automation.yaml                           # HA automation example
```

## Configuration

The service uses `appsettings.json` for configuration:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "HomeAssistantWindowsVolumeSync": "Debug"
    }
  },
  "HomeAssistant": {
    "WebhookUrl": "https://your-ha-url/api/webhook/homeassistant_windows_volume_sync",
    "TargetMediaPlayer": "media_player.your_sonos_speaker",
    "DebounceTimer": 100,
    "HealthCheckTimer": 5000,
    "HealthCheckRetries": 3
  }
}
```

### Configuration Properties

- **WebhookUrl**: Full Home Assistant webhook URL
- **TargetMediaPlayer**: Media player entity ID in Home Assistant
- **DebounceTimer**: Debounce time in milliseconds (default: 100). Time to wait after the last volume change before sending the update to Home Assistant. This prevents flooding Home Assistant with requests during rapid volume changes.
- **HealthCheckTimer**: Health check interval in milliseconds (default: 5000). Time between connection health checks to Home Assistant. The service monitors the connection and displays "Connected" or "Error" status in the system tray.
- **HealthCheckRetries**: Number of consecutive health check failures before marking the connection as disconnected (default: 3). For example, with default settings of HealthCheckTimer=5000 and HealthCheckRetries=3, the service will display "Error" status after 15 seconds (3 failures × 5 seconds) of connectivity issues.

### Logging Configuration

- Configure log levels in `appsettings.json`
- Use different levels for Development vs Production
- Console, Debug, and EventLog providers are available
- Structured logging with appropriate log levels

## Building and Running

```bash
# Build entire solution
dotnet build HomeAssistantWindowsVolumeSync.sln

# Build in Release mode
dotnet build HomeAssistantWindowsVolumeSync.sln --configuration Release

# Run all tests
dotnet test HomeAssistantWindowsVolumeSync.sln

# Run tests with detailed output
dotnet test HomeAssistantWindowsVolumeSync.sln --verbosity normal

# Publish for production
dotnet publish src -c Release -r win-x64 --self-contained false -o publish
```

## MANDATORY: Build and Test After Changes

**CRITICAL**: After ANY code change, you MUST:

1. **Build the solution**:

   ```bash
   dotnet build HomeAssistantWindowsVolumeSync.sln
   ```

2. **Run all tests**:

   ```bash
   dotnet test HomeAssistantWindowsVolumeSync.sln
   ```

3. **Verify results**:

   - All tests must pass (34+ tests)
   - No build errors or warnings
   - Both Debug and Release configurations work

4. **If tests fail**:
   - Fix the issue immediately
   - Don't proceed until all tests pass
   - Update or add tests as needed

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

1. **Write tests first** (when adding new functionality)
2. **Implement the feature** following coding conventions
3. **Build the solution**: `dotnet build HomeAssistantWindowsVolumeSync.sln`
4. **Run all tests**: `dotnet test HomeAssistantWindowsVolumeSync.sln`
5. **Fix any failures** before considering the work complete
6. Update documentation if adding new features or configuration
7. Keep the Home Assistant automation example in sync with payload format
8. Follow existing patterns for dependency injection and logging

**Remember**: Code is not complete until it builds without errors and all tests pass!
