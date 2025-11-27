namespace HomeAssistantWindowsVolumeSync;

/// <summary>
/// Interface for the centralized application logger.
/// This provides a single, testable interface for all application logging needs.
/// </summary>
public interface IAppLogger
{
    /// <summary>
    /// Logs a debug message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional message template arguments.</param>
    void LogDebug(string message, params object[] args);

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional message template arguments.</param>
    void LogInformation(string message, params object[] args);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional message template arguments.</param>
    void LogWarning(string message, params object[] args);

    /// <summary>
    /// Logs a warning message with an exception.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional message template arguments.</param>
    void LogWarning(Exception exception, string message, params object[] args);

    /// <summary>
    /// Logs an error message with an exception.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional message template arguments.</param>
    void LogError(Exception exception, string message, params object[] args);

    /// <summary>
    /// Logs a critical error message with an exception.
    /// </summary>
    /// <param name="exception">The exception to log.</param>
    /// <param name="message">The message to log.</param>
    /// <param name="args">Optional message template arguments.</param>
    void LogCritical(Exception exception, string message, params object[] args);

    /// <summary>
    /// Logs a startup error to both file and logger.
    /// This is specifically for fatal errors that occur during application initialization.
    /// </summary>
    /// <param name="exception">The exception that caused the startup failure.</param>
    void LogStartupError(Exception exception);
}
