using HomeAssistantWindowsVolumeSync;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("HomeAssistantWindowsVolumeSync.Tests")]

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

    // Register the centralized application configuration
    builder.Services.AddSingleton<IAppConfiguration>(provider =>
    {
        var configuration = provider.GetRequiredService<IConfiguration>();
        return new AppConfiguration(configuration);
    });

    // Register the HttpClient for Home Assistant webhook calls
    builder.Services.AddHttpClient<IHomeAssistantClient, HomeAssistantClient>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(10);
    })
    .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
    {
        var appConfig = serviceProvider.GetRequiredService<IAppConfiguration>();
        var strictTls = appConfig.StrictTLS;

        var handler = new HttpClientHandler();

        if (!strictTls)
        {
            // Allow any certificate when StrictTLS is disabled
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;

            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogWarning("StrictTLS is disabled. Certificate validation is bypassed for Home Assistant connections.");
        }

        return handler;
    });

    // Register the volume watcher service
    builder.Services.AddSingleton<VolumeWatcherService>();
    builder.Services.AddHostedService(provider => provider.GetRequiredService<VolumeWatcherService>());

    // Register the health check service
    builder.Services.AddSingleton<HealthCheckService>();
    builder.Services.AddSingleton<IHealthCheckService>(provider => provider.GetRequiredService<HealthCheckService>());
    builder.Services.AddHostedService(provider => provider.GetRequiredService<HealthCheckService>());

    // Register the system tray service
    builder.Services.AddHostedService<SystemTrayService>();

    // Register the centralized application logger
    builder.Services.AddSingleton<IAppLogger>(provider =>
    {
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("HomeAssistantWindowsVolumeSync");
        return new AppLogger(logger);
    });

    var host = builder.Build();

    var appLogger = host.Services.GetRequiredService<IAppLogger>();

    appLogger.LogInformation("Starting HomeAssistant Windows Volume Sync application");
    await host.RunAsync();
    appLogger.LogInformation("HomeAssistant Windows Volume Sync application stopped");
}
catch (Exception ex)
{
    // Create a fallback logger for startup errors when DI container is not available
    var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
    var logger = loggerFactory.CreateLogger("HomeAssistantWindowsVolumeSync");
    var appLogger = new AppLogger(logger);

    appLogger.LogStartupError(ex);
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
