using HomeAssistantWindowsVolumeSync;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

// For debugging: allocate a console window if running as WinExe
#if DEBUG
if (Environment.UserInteractive)
{
    NativeMethods.AllocConsole();
}
#endif

try
{
    var builder = Host.CreateApplicationBuilder(args);

    // Configure logging from appsettings.json
    builder.Logging.ClearProviders();

    // Add console logging (useful for debugging and when running interactively)
    builder.Logging.AddConsole();

    // Add debug logging (outputs to debugger window)
    builder.Logging.AddDebug();

    // Add file logging for persistent logs
    var logPath = Path.Combine(AppContext.BaseDirectory, "logs");
    if (!Directory.Exists(logPath))
    {
        Directory.CreateDirectory(logPath);
    }

    // Register the HttpClient for Home Assistant webhook calls
    builder.Services.AddHttpClient<IHomeAssistantClient, HomeAssistantClient>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(10);
    });

    // Register the volume watcher service
    builder.Services.AddSingleton<VolumeWatcherService>();
    builder.Services.AddHostedService(provider => provider.GetRequiredService<VolumeWatcherService>());

    // Register the system tray service
    builder.Services.AddHostedService<SystemTrayService>();

    var host = builder.Build();

    var logger = host.Services.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Starting HomeAssistant Windows Volume Sync application");
    await host.RunAsync();
    logger.LogInformation("HomeAssistant Windows Volume Sync application stopped");
}
catch (Exception ex)
{
    // Log to file when logger is not available (startup failures)
    var logPath = Path.Combine(AppContext.BaseDirectory, "startup-error.log");
    var errorMessage = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC - FATAL ERROR during startup:{Environment.NewLine}{ex}{Environment.NewLine}";

    try
    {
        File.AppendAllText(logPath, errorMessage);
    }
    catch
    {
        // If we can't write to file, at least try console
        Console.Error.WriteLine(errorMessage);
    }

    throw;
}

#if DEBUG
internal static class NativeMethods
{
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool AllocConsole();
}
#endif
