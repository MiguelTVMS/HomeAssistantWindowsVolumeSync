using Xunit;

namespace HomeAssistantWindowsVolumeSync.Tests;

public class ConfigurationPathsTests
{
    [Fact]
    public void GetUserConfigDirectory_ReturnsPathUnderAppData()
    {
        // Act
        var dir = ConfigurationPaths.GetUserConfigDirectory();

        // Assert
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        Assert.StartsWith(appData, dir, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("HomeAssistantWindowsVolumeSync", dir);
    }

    [Fact]
    public void GetUserConfigFilePath_ReturnsAppsettingsJsonUnderUserConfigDirectory()
    {
        // Act
        var filePath = ConfigurationPaths.GetUserConfigFilePath();

        // Assert
        Assert.Equal(
            Path.Combine(ConfigurationPaths.GetUserConfigDirectory(), "appsettings.json"),
            filePath);
    }

    [Fact]
    public void GetDefaultConfigFilePath_ReturnsPathUnderAppContextBaseDirectory()
    {
        // Act
        var filePath = ConfigurationPaths.GetDefaultConfigFilePath();

        // Assert
        Assert.Equal(
            Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
            filePath);
    }

    [Fact]
    public void EnsureUserConfigExists_CreatesDirectory_WhenItDoesNotExist()
    {
        // Arrange — redirect to a temp location to avoid touching real %APPDATA%
        using var tempScope = new TempAppDataScope();

        // Act
        tempScope.EnsureUserConfigExists();

        // Assert
        Assert.True(Directory.Exists(tempScope.UserConfigDirectory));
    }

    [Fact]
    public void EnsureUserConfigExists_CreatesConfigFile_WhenNeitherExists()
    {
        // Arrange
        using var tempScope = new TempAppDataScope();

        // Act
        tempScope.EnsureUserConfigExists();

        // Assert
        Assert.True(File.Exists(tempScope.UserConfigFilePath));
    }

    [Fact]
    public void EnsureUserConfigExists_SeedsFromDefault_WhenDefaultExists()
    {
        // Arrange
        using var tempScope = new TempAppDataScope();
        var expectedContent = @"{ ""HomeAssistant"": { ""WebhookUrl"": ""https://default.local"" } }";
        File.WriteAllText(tempScope.DefaultConfigFilePath, expectedContent);

        // Act
        tempScope.EnsureUserConfigExists();

        // Assert
        var actual = File.ReadAllText(tempScope.UserConfigFilePath);
        Assert.Equal(expectedContent, actual);
    }

    [Fact]
    public void EnsureUserConfigExists_WritesEmptyJson_WhenNoDefaultExists()
    {
        // Arrange
        using var tempScope = new TempAppDataScope(createDefaultConfig: false);

        // Act
        tempScope.EnsureUserConfigExists();

        // Assert
        var content = File.ReadAllText(tempScope.UserConfigFilePath);
        Assert.Equal("{}", content);
    }

    [Fact]
    public void EnsureUserConfigExists_DoesNotOverwriteExistingUserConfig()
    {
        // Arrange
        using var tempScope = new TempAppDataScope();
        Directory.CreateDirectory(tempScope.UserConfigDirectory);
        var existingContent = @"{ ""HomeAssistant"": { ""WebhookUrl"": ""https://user.local"" } }";
        File.WriteAllText(tempScope.UserConfigFilePath, existingContent);

        var defaultContent = @"{ ""HomeAssistant"": { ""WebhookUrl"": ""https://default.local"" } }";
        File.WriteAllText(tempScope.DefaultConfigFilePath, defaultContent);

        // Act
        tempScope.EnsureUserConfigExists();

        // Assert — user config must not be overwritten
        var actual = File.ReadAllText(tempScope.UserConfigFilePath);
        Assert.Equal(existingContent, actual);
    }

    /// <summary>
    /// Redirects ConfigurationPaths to a temp directory for isolated testing
    /// without touching the real %APPDATA% or AppContext.BaseDirectory.
    /// </summary>
    private sealed class TempAppDataScope : IDisposable
    {
        private readonly string _tempRoot;

        public string UserConfigDirectory { get; }
        public string UserConfigFilePath { get; }
        public string DefaultConfigFilePath { get; }

        public TempAppDataScope(bool createDefaultConfig = true)
        {
            _tempRoot = Path.Combine(Path.GetTempPath(), $"ConfigPathsTests_{Guid.NewGuid()}");
            UserConfigDirectory = Path.Combine(_tempRoot, "appdata", "HomeAssistantWindowsVolumeSync");
            UserConfigFilePath = Path.Combine(UserConfigDirectory, "appsettings.json");
            DefaultConfigFilePath = Path.Combine(_tempRoot, "installdir", "appsettings.json");

            Directory.CreateDirectory(Path.Combine(_tempRoot, "installdir"));

            if (createDefaultConfig)
                File.WriteAllText(DefaultConfigFilePath, "{}");
        }

        /// <summary>Calls the equivalent of ConfigurationPaths.EnsureUserConfigExists() using temp paths.</summary>
        public void EnsureUserConfigExists()
        {
            Directory.CreateDirectory(UserConfigDirectory);

            if (!File.Exists(UserConfigFilePath))
            {
                if (File.Exists(DefaultConfigFilePath))
                    File.Copy(DefaultConfigFilePath, UserConfigFilePath);
                else
                    File.WriteAllText(UserConfigFilePath, "{}");
            }
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, true);
        }
    }
}
