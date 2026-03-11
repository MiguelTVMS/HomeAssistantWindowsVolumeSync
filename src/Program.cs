using HomeAssistantWindowsVolumeSync;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
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
    // Ensure %APPDATA%\HomeAssistantWindowsVolumeSync\appsettings.json exists.
    // Seeds it from the default shipped config on first run.
    ConfigurationPaths.EnsureUserConfigExists();

    var builder = Host.CreateApplicationBuilder(args);

    // Insert the user config from %APPDATA% BEFORE environment variables so that the
    // precedence order is (lowest → highest):
    //   shipped appsettings.json  <  user config (%APPDATA%)  <  env vars  <  CLI args
    // This allows the app to work when installed to a read-only location (e.g. Program
    // Files) while still letting env vars / CLI args override for ops/debug scenarios.
    var sources = builder.Configuration.Sources;

    // Build the user config source with an explicit PhysicalFileProvider so it resolves
    // against %APPDATA%, not AppContext.BaseDirectory (which is the install directory).
    // Without this, JsonConfigurationSource.FileProvider defaults to null and the main
    // builder resolves it using its own base path (the install dir), causing the user
    // config to be silently ignored.
    var userConfigDirectory = Path.GetDirectoryName(ConfigurationPaths.GetUserConfigFilePath())!;
    var userConfigSource = new JsonConfigurationSource
    {
        Path = Path.GetFileName(ConfigurationPaths.GetUserConfigFilePath()),
        Optional = true,
        ReloadOnChange = true,
        FileProvider = new PhysicalFileProvider(userConfigDirectory)
    };

    // Insert before the first EnvironmentVariables source (after all shipped JSON files).
    var envVarIndex = sources
        .Select((s, i) => (s, i))
        .Where(x => x.s is Microsoft.Extensions.Configuration.EnvironmentVariables.EnvironmentVariablesConfigurationSource)
        .Select(x => (int?)x.i)
        .FirstOrDefault() ?? sources.Count;

    sources.Insert(envVarIndex, userConfigSource);

    // Configure logging from appsettings.json
    builder.Logging.ClearProviders();

    // Add console logging (useful for debugging and when running interactively)
    builder.Logging.AddConsole();

    // Add debug logging (outputs to debugger window)
    builder.Logging.AddDebug();

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
