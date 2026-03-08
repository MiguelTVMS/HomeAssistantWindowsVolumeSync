using Xunit;

namespace HomeAssistantWindowsVolumeSync.Tests;

/// <summary>
/// Marks a test as Windows-only.
/// - On Windows: runs normally.
/// - On macOS / Linux: skipped automatically.
/// - Tagged with Category=Windows for filter-based playlists.
/// </summary>
public sealed class WindowsFactAttribute : FactAttribute
{
    public WindowsFactAttribute()
    {
        if (!OperatingSystem.IsWindows())
            Skip = "Windows-only: requires Windows runtime (Registry, WinForms, NAudio).";
    }
}

/// <summary>
/// Marks a theory as Windows-only.
/// - On Windows: runs normally.
/// - On macOS / Linux: skipped automatically.
/// - Tagged with Category=Windows for filter-based playlists.
/// </summary>
public sealed class WindowsTheoryAttribute : TheoryAttribute
{
    public WindowsTheoryAttribute()
    {
        if (!OperatingSystem.IsWindows())
            Skip = "Windows-only: requires Windows runtime (Registry, WinForms, NAudio).";
    }
}
