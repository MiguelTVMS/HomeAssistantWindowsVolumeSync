using Xunit;

namespace HomeAssistantWindowsVolumeSync.Tests;

public class ConfigurationPathsTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _userConfigDirectory;
    private readonly string _userConfigFilePath;
    private readonly string _defaultConfigFilePath;

    public ConfigurationPathsTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"ConfigPathsTests_{Guid.NewGuid()}");
        _userConfigDirectory = Path.Combine(_tempRoot, "appdata", "HomeAssistantWindowsVolumeSync");
        _userConfigFilePath = Path.Combine(_userConfigDirectory, "appsettings.json");
        _defaultConfigFilePath = Path.Combine(_tempRoot, "installdir", "appsettings.json");
        Directory.CreateDirectory(Path.Combine(_tempRoot, "installdir"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
            Directory.Delete(_tempRoot, true);
    }

    // ---------------------------------------------------------------------------
    // Path resolution
    // ---------------------------------------------------------------------------

    [Fact]
    public void GetUserConfigDirectory_ReturnsPathUnderAppData()
    {
        var dir = ConfigurationPaths.GetUserConfigDirectory();

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        Assert.StartsWith(appData, dir, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("HomeAssistantWindowsVolumeSync", dir);
    }

    [Fact]
    public void GetUserConfigFilePath_ReturnsAppsettingsJsonUnderUserConfigDirectory()
    {
        var filePath = ConfigurationPaths.GetUserConfigFilePath();

        Assert.Equal(
            Path.Combine(ConfigurationPaths.GetUserConfigDirectory(), "appsettings.json"),
            filePath);
    }

    [Fact]
    public void GetDefaultConfigFilePath_ReturnsPathUnderAppContextBaseDirectory()
    {
        var filePath = ConfigurationPaths.GetDefaultConfigFilePath();

        Assert.Equal(
            Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
            filePath);
    }

    // ---------------------------------------------------------------------------
    // EnsureUserConfigExists — exercising the real internal overload with temp paths
    // ---------------------------------------------------------------------------

    [Fact]
    public void EnsureUserConfigExists_CreatesDirectory_WhenItDoesNotExist()
    {
        ConfigurationPaths.EnsureUserConfigExists(_userConfigDirectory, _defaultConfigFilePath);

        Assert.True(Directory.Exists(_userConfigDirectory));
    }

    [Fact]
    public void EnsureUserConfigExists_CreatesConfigFile_WhenNeitherExists()
    {
        ConfigurationPaths.EnsureUserConfigExists(_userConfigDirectory, _defaultConfigFilePath);

        Assert.True(File.Exists(_userConfigFilePath));
    }

    [Fact]
    public void EnsureUserConfigExists_SeedsFromDefault_WhenDefaultExists()
    {
        var expectedContent = @"{ ""HomeAssistant"": { ""WebhookUrl"": ""https://default.local"" } }";
        File.WriteAllText(_defaultConfigFilePath, expectedContent);

        ConfigurationPaths.EnsureUserConfigExists(_userConfigDirectory, _defaultConfigFilePath);

        var actual = File.ReadAllText(_userConfigFilePath);
        Assert.Equal(expectedContent, actual);
    }

    [Fact]
    public void EnsureUserConfigExists_WritesEmptyJson_WhenNoDefaultExists()
    {
        // Default config file deliberately not created

        ConfigurationPaths.EnsureUserConfigExists(_userConfigDirectory, _defaultConfigFilePath);

        var content = File.ReadAllText(_userConfigFilePath);
        Assert.Equal("{}", content);
    }

    [Fact]
    public void EnsureUserConfigExists_DoesNotOverwriteExistingUserConfig()
    {
        Directory.CreateDirectory(_userConfigDirectory);
        var existingContent = @"{ ""HomeAssistant"": { ""WebhookUrl"": ""https://user.local"" } }";
        File.WriteAllText(_userConfigFilePath, existingContent);

        var defaultContent = @"{ ""HomeAssistant"": { ""WebhookUrl"": ""https://default.local"" } }";
        File.WriteAllText(_defaultConfigFilePath, defaultContent);

        ConfigurationPaths.EnsureUserConfigExists(_userConfigDirectory, _defaultConfigFilePath);

        var actual = File.ReadAllText(_userConfigFilePath);
        Assert.Equal(existingContent, actual);
    }

    [Fact]
    public void EnsureUserConfigExists_IsIdempotent_WhenCalledMultipleTimes()
    {
        File.WriteAllText(_defaultConfigFilePath, "{}");

        ConfigurationPaths.EnsureUserConfigExists(_userConfigDirectory, _defaultConfigFilePath);
        ConfigurationPaths.EnsureUserConfigExists(_userConfigDirectory, _defaultConfigFilePath);
        ConfigurationPaths.EnsureUserConfigExists(_userConfigDirectory, _defaultConfigFilePath);

        Assert.True(File.Exists(_userConfigFilePath));
        Assert.Single(Directory.GetFiles(_userConfigDirectory));
    }

    // ---------------------------------------------------------------------------
    // Public EnsureUserConfigExists() — production overload coverage
    // ---------------------------------------------------------------------------

    [Fact]
    public void EnsureUserConfigExists_PublicOverload_CreatesUserConfigDirectory()
    {
        // The public overload must create the real %APPDATA% directory and config file.
        // We verify this without writing test data — just assert the directory and file
        // exist after calling it (idempotent: safe to call when config already exists).
        ConfigurationPaths.EnsureUserConfigExists();

        Assert.True(Directory.Exists(ConfigurationPaths.GetUserConfigDirectory()));
        Assert.True(File.Exists(ConfigurationPaths.GetUserConfigFilePath()));
    }
}
