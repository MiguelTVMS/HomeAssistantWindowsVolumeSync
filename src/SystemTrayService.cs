using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows.Forms;

namespace HomeAssistantWindowsVolumeSync;

/// <summary>
/// Background service that manages the system tray icon with pause/resume functionality.
/// </summary>
public class SystemTrayService : BackgroundService
{
    private readonly ILogger<SystemTrayService> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IServiceProvider _serviceProvider;
    private readonly IAppConfiguration _configuration;
    private VolumeWatcherService? _volumeWatcherService;
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;
    private ToolStripMenuItem? _pauseResumeMenuItem;
    private ToolStripMenuItem? _statusMenuItem;
    private bool _isPaused;

    public SystemTrayService(
        ILogger<SystemTrayService> logger,
        IHostApplicationLifetime lifetime,
        IServiceProvider serviceProvider,
        IAppConfiguration configuration)
    {
        _logger = logger;
        _lifetime = lifetime;
        _serviceProvider = serviceProvider;
        _configuration = configuration;

        _logger.LogInformation("SystemTrayService constructor called");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SystemTrayService is starting...");

        // Resolve VolumeWatcherService from the service provider
        _volumeWatcherService = _serviceProvider.GetRequiredService<VolumeWatcherService>();
        _logger.LogInformation("VolumeWatcherService resolved successfully");

        // Initialize the system tray icon on the UI thread
        var tcs = new TaskCompletionSource();
        var thread = new Thread(() =>
        {
            try
            {
                InitializeTrayIcon();
                tcs.SetResult();

                // Run the message loop
                Application.Run();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in system tray UI thread");
                tcs.SetException(ex);
            }
            finally
            {
                // Clean up when the message loop exits
                _notifyIcon?.Dispose();
                _contextMenu?.Dispose();
            }
        })
        {
            IsBackground = false,
            Name = "SystemTrayThread"
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        await tcs.Task;
        _logger.LogInformation("SystemTrayService started successfully");

        // Wait for cancellation, then exit the message loop
        stoppingToken.Register(() =>
        {
            _logger.LogInformation("SystemTrayService is stopping...");
            try
            {
                // Exit the application message loop
                Application.ExitThread();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping system tray");
            }
        });

        // Keep the service running until cancellation is requested
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping
        }
    }

    private void InitializeTrayIcon()
    {
        // Create context menu
        _contextMenu = new ContextMenuStrip();

        // Status menu item (disabled, shows current state)
        _statusMenuItem = new ToolStripMenuItem("Status: Running")
        {
            Enabled = false
        };
        _contextMenu.Items.Add(_statusMenuItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        // Pause/Resume menu item
        _pauseResumeMenuItem = new ToolStripMenuItem("Pause", null, OnPauseResumeClick);
        _contextMenu.Items.Add(_pauseResumeMenuItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        // Settings menu item
        var settingsMenuItem = new ToolStripMenuItem("Settings", null, OnSettingsClick);
        _contextMenu.Items.Add(settingsMenuItem);

        _contextMenu.Items.Add(new ToolStripSeparator());

        // Exit menu item
        var exitMenuItem = new ToolStripMenuItem("Exit", null, OnExitClick);
        _contextMenu.Items.Add(exitMenuItem);

        // Create notify icon
        _notifyIcon = new NotifyIcon
        {
            Text = "Home Assistant Windows Volume Sync",
            ContextMenuStrip = _contextMenu,
            Visible = true
        };

        // Load icon from embedded resource or file
        try
        {
            var iconPath = Path.Combine(AppContext.BaseDirectory, "app.ico");
            var pngPath = Path.Combine(AppContext.BaseDirectory, "app.png");

            if (File.Exists(iconPath))
            {
                try
                {
                    _notifyIcon.Icon = new System.Drawing.Icon(iconPath);
                    _logger.LogInformation("Loaded icon from {IconPath}", iconPath);
                }
                catch
                {
                    // If ICO fails, try PNG
                    if (File.Exists(pngPath))
                    {
                        using var bitmap = new System.Drawing.Bitmap(pngPath);
                        var iconHandle = bitmap.GetHicon();
                        _notifyIcon.Icon = System.Drawing.Icon.FromHandle(iconHandle);
                        _logger.LogInformation("Loaded icon from {PngPath}", pngPath);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            else if (File.Exists(pngPath))
            {
                // Load PNG and convert to icon
                using var bitmap = new System.Drawing.Bitmap(pngPath);
                var iconHandle = bitmap.GetHicon();
                _notifyIcon.Icon = System.Drawing.Icon.FromHandle(iconHandle);
                _logger.LogInformation("Loaded icon from {PngPath}", pngPath);
            }
            else
            {
                _logger.LogWarning("Icon file not found at {IconPath} or {PngPath}. Using default icon.", iconPath, pngPath);
                // Use a default system icon if the custom icon is not found
                _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load custom icon. Using default icon.");
            _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
        }

        // Double-click to show status
        _notifyIcon.DoubleClick += (s, e) =>
        {
            var status = _isPaused ? "Paused" : "Running";
            MessageBox.Show(
                $"Home Assistant Windows Volume Sync\n\nStatus: {status}",
                "Volume Sync Status",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        };

        _logger.LogInformation("System tray icon initialized successfully");
    }

    private void OnPauseResumeClick(object? sender, EventArgs e)
    {
        _isPaused = !_isPaused;

        if (_pauseResumeMenuItem != null)
        {
            _pauseResumeMenuItem.Text = _isPaused ? "Resume" : "Pause";
        }

        if (_statusMenuItem != null)
        {
            _statusMenuItem.Text = _isPaused ? "Status: Paused" : "Status: Running";
        }

        if (_notifyIcon != null)
        {
            _notifyIcon.Text = _isPaused
                ? "Home Assistant Windows Volume Sync (Paused)"
                : "Home Assistant Windows Volume Sync";
        }

        // Update the volume watcher service pause state
        if (_volumeWatcherService != null)
        {
            _volumeWatcherService.SetPaused(_isPaused);
        }

        _logger.LogInformation("Volume sync {Status}", _isPaused ? "paused" : "resumed");
    }

    private void OnSettingsClick(object? sender, EventArgs e)
    {
        _logger.LogInformation("Settings requested from system tray");

        try
        {
            var settingsManager = new SettingsManager(_configuration);
            using var settingsForm = new SettingsForm(
                _configuration,
                (url, id, player) => settingsManager.SaveSettings(url, id, player));

            var result = settingsForm.ShowDialog();

            if (result == DialogResult.OK)
            {
                _logger.LogInformation("Settings updated successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing settings dialog");
            MessageBox.Show(
                $"Failed to open settings:\n{ex.Message}",
                "Settings Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void OnExitClick(object? sender, EventArgs e)
    {
        _logger.LogInformation("Exit requested from system tray");
        _lifetime.StopApplication();
    }

    public override void Dispose()
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }

        _contextMenu?.Dispose();

        base.Dispose();
    }
}
