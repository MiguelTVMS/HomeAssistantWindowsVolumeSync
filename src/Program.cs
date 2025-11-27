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

var builder = Host.CreateApplicationBuilder(args);

// Add console logging to see output
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// Configure Windows Service hosting
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "HomeAssistant Windows Volume Sync";
});

// Register the HttpClient for Home Assistant webhook calls
builder.Services.AddHttpClient<IHomeAssistantClient, HomeAssistantClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Register the volume watcher service
builder.Services.AddSingleton<VolumeWatcherService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<VolumeWatcherService>());

// Register the system tray service
Console.WriteLine("Registering SystemTrayService...");
builder.Services.AddHostedService<SystemTrayService>();

try
{
    Console.WriteLine("Building host...");
    var host = builder.Build();

    Console.WriteLine("Starting host...");
    Console.WriteLine("Services registered:");
    var hostedServices = host.Services.GetServices<IHostedService>();
    foreach (var service in hostedServices)
    {
        Console.WriteLine($"  - {service.GetType().Name}");
    }

    await host.RunAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"FATAL ERROR: {ex}");
    var logPath = Path.Combine(AppContext.BaseDirectory, "startup-error.log");
    File.WriteAllText(logPath, $"{DateTime.Now}: {ex}");
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
