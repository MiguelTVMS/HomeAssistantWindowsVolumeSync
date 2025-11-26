using HomeAssistantWindowsVolumeSync;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

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
builder.Services.AddHostedService<VolumeWatcherService>();

var host = builder.Build();
host.Run();
