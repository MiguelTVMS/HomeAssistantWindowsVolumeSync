# AI Agent Instructions for HomeAssistantWindowsVolumeSync

## Project Overview

HomeAssistantWindowsVolumeSync is a Windows system tray application that synchronizes the Windows system master volume with Home Assistant via webhooks. It uses NAudio to subscribe to volume change events and sends updates in real-time.

## Technology Stack

- **Framework**: .NET 8.0 (Windows)
- **Application Type**: Windows tray application using WinForms (`UseWindowsForms`) and `Microsoft.Extensions.Hosting`
- **Audio Library**: NAudio for Windows Core Audio API access
- **HTTP Client**: `System.Net.Http` with `IHttpClientFactory`
- **Testing**: xUnit with Moq for mocking

## First-Time Setup (per clone)

After cloning, activate the pre-commit hook that validates AI instruction symlinks:

```bash
git config core.hooksPath .githooks
```

This prevents accidental commits that break `AGENTS.md`, `.github/copilot-instructions.md`, or `.github/AGENTS.md` — all of which must remain symlinks pointing to `CLAUDE.md`.

To run the validation manually at any time:

```bash
bash scripts/validate-symlinks.sh
```

## Branching Strategy (GitFlow)

This repository follows **GitFlow**. All branches must be created accordingly — no exceptions.

### Permanent branches

| Branch | Purpose |
|--------|---------|
| `main` | Production-ready releases only. Never commit directly. |
| `develop` | Integration branch. All feature/fix work merges here first. |

### Working branches

| Type | Pattern | Branch from | Merges into |
|------|---------|-------------|-------------|
| Feature | `feature/<short-description>` | `develop` | `develop` |
| Bug fix | `fix/<short-description>` | `develop` | `develop` |
| Release | `release/<version>` | `develop` | `main` + `develop` |
| Hotfix | `hotfix/<short-description>` | `main` | `main` + `develop` |

### Rules

- **Never commit directly to `main` or `develop`** — always work on a branch and open a PR
- Feature branches off `develop`, not `main`
- Hotfixes are the only branches allowed to branch off `main` (production emergencies only)
- Release branches are used to prepare a version — bump version, update changelog, final QA — then merge to both `main` and `develop`
- Delete branches after merging

### Examples

```bash
# New feature
git checkout develop
git checkout -b feature/add-wix-installer

# Bug fix
git checkout develop
git checkout -b fix/tray-icon-leak-on-disconnect

# Hotfix on production
git checkout main
git checkout -b hotfix/crash-on-missing-appsettings

# Release prep
git checkout develop
git checkout -b release/1.2.0
```

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
    "TargetMediaPlayer": "media_player.your_media_player",
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

## Test Playlists

Tests that require Windows APIs are tagged with `[WindowsFact]` / `[WindowsTheory]` (defined in `tests/WindowsOnlyAttributes.cs`). These attributes:

- Automatically **skip** on macOS / Linux with a clear message
- Emit **`Category=Windows`** trait for filter-based playlists via xUnit's `ITraitAttribute` / `ITraitDiscoverer`

### When to use

| Attribute | Use instead of | When |
|-----------|---------------|------|
| `[WindowsFact]` | `[Fact]` | Test uses Registry, WinForms, NAudio COM types (`MMDevice`, `MMDeviceEnumerator`), or any Windows-only API |
| `[WindowsTheory]` | `[Theory]` | Same, but for data-driven tests |
| `[Fact]` | `[WindowsFact]` | Test exercises **pure logic only** — no Windows/COM/NAudio type references (e.g. delegate-injection tests using generic or string stubs) |

### Filter commands (playlists)

```bash
# Run all tests (requires Windows runtime)
dotnet test HomeAssistantWindowsVolumeSync.sln

# Run Windows-specific tests only
dotnet test HomeAssistantWindowsVolumeSync.sln --filter "Category=Windows"

# Run cross-platform tests only (skip Windows-specific)
dotnet test HomeAssistantWindowsVolumeSync.sln --filter "Category!=Windows"
```

### Currently tagged Windows-only test files

| File | Reason |
|------|--------|
| `WindowsStartupManagerTests.cs` | Uses `Registry.CurrentUser` |
| `IWindowsStartupManagerTests.cs` | Calls `WindowsStartupManager` (Registry) |
| `SystemTrayServiceTests.cs` | Requires WinForms / `NotifyIcon` |
| `VolumeWatcherServiceTests.cs` | Mixed: most tests use NAudio (`[WindowsFact]`); `ResolveMonitoredDevice` tests use string stubs and are `[Fact]` |

### NAudio COM types are sealed — use delegate injection for testability

`MMDevice` and `MMDeviceEnumerator` are **sealed COM classes** and cannot be constructed or mocked without a real Windows audio stack. Any method that calls these types directly is untestable without the COM runtime.

**Pattern: delegate injection with a generic type parameter**

When a method needs to be unit-tested without COM, extract the logic into a `static` (or standalone) method that accepts delegates returning a generic `TDevice` instead of `MMDevice`, plus a `getName` delegate to get the device name:

```csharp
internal static TDevice? ResolveSomething<TDevice>(
    Func<string, TDevice?> getDevice,
    Func<TDevice?> getDefault,
    Func<TDevice, string?> getName,
    Action<COMException, string> logWarning,
    Action<string, string?> logInfo)
    where TDevice : class
{
    // pure logic — no COM references
}
```

The production call site passes real NAudio delegates:

```csharp
ResolveMonitoredDevice(
    configuredDeviceId,
    id => (MMDevice?)_enumerator.GetDevice(id),
    () => (MMDevice?)_enumerator.GetDefaultAudioEndpoint(...),
    d => d.FriendlyName,
    logWarning,
    logInfo);
```

Tests use `string` (or any non-COM stub):

```csharp
VolumeWatcherService.ResolveMonitoredDevice(
    configuredId,
    id => "Headphones",
    () => "Speakers",
    d => d,           // name == the stub string itself
    (ex, id) => { },
    (kind, name) => { loggedKind = kind; });
```

**Never** return `null!` or use `stubDevice!` null-forgiveness in tests as a workaround for sealed COM types — it masks the real problem. Extract logic with delegate injection instead.

## Platform Constraints

This project targets `net8.0-windows`. The `Microsoft.WindowsDesktop.App` runtime only exists on Windows, so:

- **`dotnet test` cannot run on macOS or Linux** — the test host will abort with a missing framework error
- If the current environment is not Windows, skip local test execution and rely on CI (`windows-latest` runner) as the verification gate
- Always note in the PR that local tests were skipped due to platform constraints

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

The application sends JSON payloads to Home Assistant:

```json
{
  "volume": 50,
  "mute": false,
  "target_media_player": "media_player.your_media_player"
}
```

### Application Lifecycle

1. Application starts and initializes audio device monitoring
2. Initial volume state is sent to Home Assistant
3. Volume changes trigger webhook calls
4. Application gracefully handles cancellation and disposes resources

## Pull Request Process

### Copilot Review — Mandatory Before Merging

**Never merge a PR without waiting for GitHub Copilot to post its review.**

Copilot reviews are triggered automatically when CI completes. After CI goes green:

1. Check `GET /repos/{owner}/{repo}/pulls/{n}/reviews` — wait until a new review appears with a `submitted_at` timestamp after your last push
2. Check `GET /repos/{owner}/{repo}/pulls/{n}/comments` — read all inline comments from that review
3. If there are inline comments, act on all of them in a follow-up commit
4. Only merge when Copilot has reviewed the latest commit **and** left no unresolved inline comments

### Commit Standards

- Write descriptive commit messages (`fix:`, `feat:`, `chore:`, `docs:` prefixes)
- Each commit should be a logical unit; squash noise before opening a PR
- Pre-commit hook validates symlinks — fix failures before pushing

### Before Opening a PR

- `npm run check` / `dotnet build` — no errors or warnings
- All tests pass
- Symlinks validated (`bash scripts/validate-symlinks.sh`)
- PR description explains what changed and why

1. **Write tests first** (when adding new functionality)
2. **Implement the feature** following coding conventions
3. **Build the solution**: `dotnet build HomeAssistantWindowsVolumeSync.sln`
4. **Run all tests**: `dotnet test HomeAssistantWindowsVolumeSync.sln`
5. **Fix any failures** before considering the work complete
6. Update documentation if adding new features or configuration
7. Keep the Home Assistant automation example in sync with payload format
8. Follow existing patterns for dependency injection and logging

**Remember**: Code is not complete until it builds without errors and all tests pass!
