using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HomeAssistantWindowsVolumeSync.Tests;

/// <summary>
/// Contract tests for IAppLogger interface to ensure implementations follow expected behavior.
/// </summary>
public class IAppLoggerTests
{
    [Fact]
    public void IAppLogger_HasLogDebugMethod()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        IAppLogger appLogger = new AppLogger(mockLogger.Object);

        // Act & Assert - Method should exist and be callable
        appLogger.LogDebug("Test message");
    }

    [Fact]
    public void IAppLogger_HasLogInformationMethod()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        IAppLogger appLogger = new AppLogger(mockLogger.Object);

        // Act & Assert - Method should exist and be callable
        appLogger.LogInformation("Test message");
    }

    [Fact]
    public void IAppLogger_HasLogWarningMethod()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        IAppLogger appLogger = new AppLogger(mockLogger.Object);

        // Act & Assert - Method should exist and be callable
        appLogger.LogWarning("Test message");
    }

    [Fact]
    public void IAppLogger_HasLogWarningMethodWithException()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        IAppLogger appLogger = new AppLogger(mockLogger.Object);
        var exception = new Exception("Test exception");

        // Act & Assert - Method should exist and be callable
        appLogger.LogWarning(exception, "Test message");
    }

    [Fact]
    public void IAppLogger_HasLogErrorMethod()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        IAppLogger appLogger = new AppLogger(mockLogger.Object);
        var exception = new Exception("Test exception");

        // Act & Assert - Method should exist and be callable
        appLogger.LogError(exception, "Test message");
    }

    [Fact]
    public void IAppLogger_HasLogCriticalMethod()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        IAppLogger appLogger = new AppLogger(mockLogger.Object);
        var exception = new Exception("Test exception");

        // Act & Assert - Method should exist and be callable
        appLogger.LogCritical(exception, "Test message");
    }

    [Fact]
    public void IAppLogger_HasLogStartupErrorMethod()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        IAppLogger appLogger = new AppLogger(mockLogger.Object);
        var exception = new Exception("Test exception");
        var logPath = Path.Combine(AppContext.BaseDirectory, "startup-error.log");

        // Clean up before test
        if (File.Exists(logPath))
        {
            try
            {
                File.Delete(logPath);
            }
            catch (IOException)
            {
                // File might be locked, wait a bit and retry
                Thread.Sleep(100);
                try
                {
                    File.Delete(logPath);
                }
                catch
                {
                    // Ignore cleanup failures - the test can still proceed
                }
            }
        }

        try
        {
            // Act & Assert - Method should exist and be callable
            appLogger.LogStartupError(exception);

            // Give the file write a moment to complete
            Thread.Sleep(100);
        }
        finally
        {
            // Clean up after test - best effort
            try
            {
                if (File.Exists(logPath))
                {
                    Thread.Sleep(100);
                    File.Delete(logPath);
                }
            }
            catch
            {
                // Ignore cleanup failures
            }
        }
    }

    [Fact]
    public void IAppLogger_SupportsParameterizedMessages()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        IAppLogger appLogger = new AppLogger(mockLogger.Object);

        // Act & Assert - Should support message templates with parameters
        appLogger.LogDebug("Test {Value}", 42);
        appLogger.LogInformation("Test {Name}", "TestName");
        appLogger.LogWarning("Test {Status}", "Warning");
    }

    [Fact]
    public void IAppLogger_Implementation_IsReusable()
    {
        // Arrange
        var mockLogger1 = new Mock<ILogger>();
        var mockLogger2 = new Mock<ILogger>();

        // Act - Create multiple instances
        IAppLogger appLogger1 = new AppLogger(mockLogger1.Object);
        IAppLogger appLogger2 = new AppLogger(mockLogger2.Object);

        appLogger1.LogInformation("Logger 1");
        appLogger2.LogInformation("Logger 2");

        // Assert - Both instances should work independently
        mockLogger1.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        mockLogger2.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void IAppLogger_CanBeUsedThroughInterface()
    {
        // Arrange
        var mockLogger = new Mock<ILogger>();
        IAppLogger appLogger = new AppLogger(mockLogger.Object);

        // Act - Use through interface reference
        UseLogger(appLogger);

        // Assert - Should have logged through interface
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static void UseLogger(IAppLogger logger)
    {
        logger.LogInformation("Used through interface");
    }
}
