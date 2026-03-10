using System.Collections.Generic;
using Xunit;
using Xunit.Sdk;

namespace HomeAssistantWindowsVolumeSync.Tests;

/// <summary>
/// xUnit v2 trait discoverer that emits Category=Windows for all
/// WindowsFact / WindowsTheory annotated tests.
/// Enables filter-based playlists:
///   dotnet test --filter "Category=Windows"
///   dotnet test --filter "Category!=Windows"
/// </summary>
public class WindowsTraitDiscoverer : ITraitDiscoverer
{
    public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
    {
        yield return new KeyValuePair<string, string>("Category", "Windows");
    }
}

/// <summary>
/// Marks a test as Windows-only.
/// - On Windows  : runs normally, tagged Category=Windows.
/// - On non-Windows : skipped automatically with a clear message.
///
/// Use instead of [Fact] for tests that require Registry, WinForms, or NAudio.
/// </summary>
[TraitDiscoverer(
    "HomeAssistantWindowsVolumeSync.Tests.WindowsTraitDiscoverer",
    "HomeAssistantWindowsVolumeSync.Tests")]
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class WindowsFactAttribute : FactAttribute, ITraitAttribute
{
    public WindowsFactAttribute()
    {
        if (!OperatingSystem.IsWindows())
            Skip = "Windows-only: requires Windows runtime (Registry, WinForms, NAudio).";
    }
}

/// <summary>
/// Marks a theory as Windows-only.
/// - On Windows  : runs normally, tagged Category=Windows.
/// - On non-Windows : skipped automatically with a clear message.
///
/// Use instead of [Theory] for tests that require Registry, WinForms, or NAudio.
/// </summary>
[TraitDiscoverer(
    "HomeAssistantWindowsVolumeSync.Tests.WindowsTraitDiscoverer",
    "HomeAssistantWindowsVolumeSync.Tests")]
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public sealed class WindowsTheoryAttribute : TheoryAttribute, ITraitAttribute
{
    public WindowsTheoryAttribute()
    {
        if (!OperatingSystem.IsWindows())
            Skip = "Windows-only: requires Windows runtime (Registry, WinForms, NAudio).";
    }
}
