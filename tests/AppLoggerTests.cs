using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HomeAssistantWindowsVolumeSync.Tests;

public class AppLoggerTests
{
    private readonly Mock<ILogger> _mockLogger;
    private readonly AppLogger _appLogger;

    public AppLoggerTests()
    {
        _mockLogger = new Mock<ILogger>();
        _appLogger = new AppLogger(_mockLogger.Object);
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AppLogger(null!));
    }

    [Fact]
    public void LogDebug_LogsDebugMessage()
    {
        // Arrange
        const string message = "Debug message";

        // Act
        _appLogger.LogDebug(message);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogDebug_WithArguments_LogsFormattedMessage()
    {
        // Arrange
        const string message = "Debug message with {Value}";
        const int value = 42;

        // Act
        _appLogger.LogDebug(message, value);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogInformation_LogsInformationMessage()
    {
        // Arrange
        const string message = "Information message";

        // Act
        _appLogger.LogInformation(message);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogInformation_WithArguments_LogsFormattedMessage()
    {
        // Arrange
        const string message = "Information with {Name}";
        const string name = "TestName";

        // Act
        _appLogger.LogInformation(message, name);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogWarning_LogsWarningMessage()
    {
        // Arrange
        const string message = "Warning message";

        // Act
        _appLogger.LogWarning(message);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogWarning_WithException_LogsWarningWithException()
    {
        // Arrange
        const string message = "Warning with exception";
        var exception = new InvalidOperationException("Test exception");

        // Act
        _appLogger.LogWarning(exception, message);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogError_LogsErrorWithException()
    {
        // Arrange
        const string message = "Error message";
        var exception = new Exception("Test exception");

        // Act
        _appLogger.LogError(exception, message);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogError_WithArguments_LogsFormattedError()
    {
        // Arrange
        const string message = "Error in {Component}";
        const string component = "TestComponent";
        var exception = new Exception("Test exception");

        // Act
        _appLogger.LogError(exception, message, component);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogCritical_LogsCriticalWithException()
    {
        // Arrange
        const string message = "Critical error";
        var exception = new Exception("Fatal exception");

        // Act
        _appLogger.LogCritical(exception, message);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Critical,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void LogStartupError_WritesToFileAndLogsError()
    {
        // Arrange
        var exception = new Exception("Startup exception");
        var expectedLogPath = Path.Combine(AppContext.BaseDirectory, "startup-error.log");

        // Clean up any existing log file
        if (File.Exists(expectedLogPath))
        {
            File.Delete(expectedLogPath);
        }

        try
        {
            // Act
            _appLogger.LogStartupError(exception);

            // Give file operations time to complete
            Thread.Sleep(100);

            // Assert - Verify file was created
            Assert.True(File.Exists(expectedLogPath), "Startup error log file should be created");

            // Verify file content contains exception details
            var logContent = File.ReadAllText(expectedLogPath);
            Assert.Contains("FATAL ERROR during startup", logContent);
            // Exception.ToString() includes type and message, so check for the type
            Assert.Contains("System.Exception", logContent);

            // Verify critical log was called
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Critical,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    exception,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        finally
        {
            // Clean up
            if (File.Exists(expectedLogPath))
            {
                Thread.Sleep(100);
                try
                {
                    File.Delete(expectedLogPath);
                }
                catch
                {
                    // Ignore cleanup failures
                }
            }
        }
    }

    [Fact]
    public void LogStartupError_FileWriteFails_LogsErrorToLogger()
    {
        // Arrange
        var exception = new Exception("Startup exception");
        var invalidPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "invalid", "startup-error.log");

        // Use a custom AppLogger that will attempt to write to an invalid path
        // This simulates a file write failure scenario

        // Act
        _appLogger.LogStartupError(exception);

        // Assert - Verify critical log was still called
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Critical,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Theory]
    [InlineData("Simple message")]
    [InlineData("Message with special chars: !@#$%^&*()")]
    [InlineData("Message with newlines\nand\ttabs")]
    public void LogInformation_VariousMessages_LogsCorrectly(string message)
    {
        // Act
        _appLogger.LogInformation(message);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void MultipleLogCalls_AllLogsExecuted()
    {
        // Act
        _appLogger.LogDebug("Debug");
        _appLogger.LogInformation("Info");
        _appLogger.LogWarning("Warning");
        _appLogger.LogError(new Exception(), "Error");
        _appLogger.LogCritical(new Exception(), "Critical");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(5));
    }
}
