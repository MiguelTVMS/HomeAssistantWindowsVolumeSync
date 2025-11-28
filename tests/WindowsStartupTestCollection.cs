using Xunit;

namespace HomeAssistantWindowsVolumeSync.Tests;

/// <summary>
/// Collection definition to ensure WindowsStartup tests run sequentially.
/// All tests that modify the Windows registry startup entry must be in this collection
/// to prevent race conditions and test interference.
/// </summary>
[CollectionDefinition("WindowsStartup", DisableParallelization = true)]
public class WindowsStartupTestCollection
{
    // This class is never instantiated. It's just a marker for xUnit.
}
