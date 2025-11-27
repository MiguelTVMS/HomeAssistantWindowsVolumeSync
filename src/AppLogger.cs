using Microsoft.Extensions.Logging;

namespace HomeAssistantWindowsVolumeSync;

/// <summary>
/// Centralized application logger that provides unified logging functionality.
/// This class wraps ILogger to ensure all logging goes through a single, testable interface.
/// </summary>
public class AppLogger : IAppLogger
{
    private readonly ILogger _logger;

    public AppLogger(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public void LogDebug(string message, params object[] args)
    {
        _logger.LogDebug(message, args);
    }

    /// <inheritdoc/>
    public void LogInformation(string message, params object[] args)
    {
        _logger.LogInformation(message, args);
    }

    /// <inheritdoc/>
    public void LogWarning(string message, params object[] args)
    {
        _logger.LogWarning(message, args);
    }

    /// <inheritdoc/>
    public void LogWarning(Exception exception, string message, params object[] args)
    {
        _logger.LogWarning(exception, message, args);
    }

    /// <inheritdoc/>
    public void LogError(Exception exception, string message, params object[] args)
    {
        _logger.LogError(exception, message, args);
    }

    /// <inheritdoc/>
    public void LogCritical(Exception exception, string message, params object[] args)
    {
        _logger.LogCritical(exception, message, args);
    }

    /// <inheritdoc/>
    public void LogStartupError(Exception exception)
    {
        var logPath = Path.Combine(AppContext.BaseDirectory, "startup-error.log");
        var errorMessage = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - FATAL ERROR during startup:{Environment.NewLine}{exception}{Environment.NewLine}";

        try
        {
            File.AppendAllText(logPath, errorMessage);
            _logger.LogCritical(exception, "Fatal error during application startup. Error details written to {LogPath}", logPath);
        }
        catch (Exception fileException)
        {
            // If we can't write to file, log the failure through the logger
            _logger.LogCritical(exception, "Fatal error during application startup");
            _logger.LogError(fileException, "Additionally, failed to write startup error to file: {LogPath}", logPath);
        }
    }
}
