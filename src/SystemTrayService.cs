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
    private IHealthCheckService? _healthCheckService;
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _contextMenu;
    private ToolStripMenuItem? _pauseResumeMenuItem;
    private ToolStripMenuItem? _statusMenuItem;
    private ToolStripMenuItem? _connectionStatusMenuItem;
    private bool _isPaused;
    private bool _isDisposed;
    private SettingsForm? _settingsForm;
    private Form? _statusDialog;

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

        // Resolve HealthCheckService from the service provider
        _healthCheckService = _serviceProvider.GetRequiredService<IHealthCheckService>();
        _healthCheckService.ConnectionStateChanged += OnConnectionStateChanged;
        _logger.LogInformation("HealthCheckService resolved successfully");

        // Initialize the system tray icon on the UI thread
        var tcs = new TaskCompletionSource();
        var thread = new Thread(() =>
        {
            try
            {
                InitializeTrayIcon();

                // Set initial connection status after tray icon is initialized
                UpdateConnectionStatus(_healthCheckService.IsConnected);

                tcs.SetResult();

                // Run the message loop
                Application.Run();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in system tray UI thread");
                tcs.SetException(ex);
            }
            // Ensure Windows Forms controls are disposed on the UI thread after Application.Run() exits.
            finally
            {
                _notifyIcon?.Dispose();
                _contextMenu?.Dispose();
                _pauseResumeMenuItem?.Dispose();
                _statusMenuItem?.Dispose();
                _connectionStatusMenuItem?.Dispose();
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
                // Exit the application message loop to unblock the UI thread
                _logger.LogDebug("Calling Application.Exit() to stop message loop");
                Application.Exit();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping system tray message loop");
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
            _logger.LogInformation("SystemTrayService cancellation requested");
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

        // Connection status menu item (disabled, shows connection state)
        _connectionStatusMenuItem = new ToolStripMenuItem("Connection: Checking...")
        {
            Enabled = false
        };
        _contextMenu.Items.Add(_connectionStatusMenuItem);

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
            ShowStatusDialog();
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

    private void ShowStatusDialog()
    {
        // Check if status dialog is already open
        if (_statusDialog != null && !_statusDialog.IsDisposed)
        {
            _logger.LogInformation("Status dialog is already open, bringing it to front");

            // Bring the existing dialog to the front
            if (_statusDialog.InvokeRequired)
            {
                _statusDialog.BeginInvoke(new Action(() =>
                {
                    _statusDialog.WindowState = FormWindowState.Normal;
                    _statusDialog.BringToFront();
                    _statusDialog.Activate();
                }));
            }
            else
            {
                _statusDialog.WindowState = FormWindowState.Normal;
                _statusDialog.BringToFront();
                _statusDialog.Activate();
            }

            return;
        }

        var status = _isPaused ? "Paused" : "Running";
        var connectionStatus = _healthCheckService?.IsConnected == true ? "Connected" : "Error";

        // Create a custom dialog with Windows standard layout
        _statusDialog = new Form
        {
            Text = "Volume Sync Status",
            ClientSize = new System.Drawing.Size(400, 150),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterScreen,
            MaximizeBox = false,
            MinimizeBox = false,
            Padding = new Padding(12) // Standard Windows dialog padding
        };

        // Handle form closing to clear the reference
        _statusDialog.FormClosed += (s, args) =>
        {
            _logger.LogInformation("Status dialog closed");
            if (_statusDialog != null)
            {
                _statusDialog.Dispose();
                _statusDialog = null;
            }
        };

        // Add icon (standard Windows layout)
        var iconPictureBox = new PictureBox
        {
            Image = SystemIcons.Information.ToBitmap(),
            SizeMode = PictureBoxSizeMode.AutoSize,
            Location = new System.Drawing.Point(12, 12)
        };
        _statusDialog.Controls.Add(iconPictureBox);

        // Add status label with proper spacing from icon
        var statusLabel = new Label
        {
            Text = $"Home Assistant Windows Volume Sync\n\nStatus: {status}\nConnection: {connectionStatus}",
            Left = iconPictureBox.Right + 12,
            Top = 12,
            AutoSize = true
        };
        _statusDialog.Controls.Add(statusLabel);

        // Create button panel at bottom with standard Windows layout
        var buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            Height = 40,
            Padding = new Padding(0, 7, 0, 0), // Standard button panel padding
            AutoSize = true,
            WrapContents = false
        };
        _statusDialog.Controls.Add(buttonPanel);

        // Add Close button (rightmost, standard Windows size)
        var closeButton = new Button
        {
            Text = "Close",
            DialogResult = DialogResult.OK,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new System.Drawing.Size(75, 23),
            Margin = new Padding(0, 0, 0, 0)
        };
        buttonPanel.Controls.Add(closeButton);

        // Add Settings button (standard Windows size, 6px gap)
        var settingsButton = new Button
        {
            Text = "Settings",
            DialogResult = DialogResult.Yes,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new System.Drawing.Size(75, 23),
            Margin = new Padding(0, 0, 6, 0)
        };
        buttonPanel.Controls.Add(settingsButton);

        // Show dialog and handle result
        var result = _statusDialog.ShowDialog();
        if (result == DialogResult.Yes)
        {
            // User clicked Settings, open settings dialog
            OnSettingsClick(null, EventArgs.Empty);
        }
    }

    private void OnSettingsClick(object? sender, EventArgs e)
    {
        _logger.LogInformation("Settings requested from system tray");

        try
        {
            // Check if settings form is already open
            if (_settingsForm != null && !_settingsForm.IsDisposed)
            {
                _logger.LogInformation("Settings form is already open, bringing it to front");

                // Bring the existing form to the front
                if (_settingsForm.InvokeRequired)
                {
                    _settingsForm.BeginInvoke(new Action(() =>
                    {
                        _settingsForm.WindowState = FormWindowState.Normal;
                        _settingsForm.BringToFront();
                        _settingsForm.Activate();
                    }));
                }
                else
                {
                    _settingsForm.WindowState = FormWindowState.Normal;
                    _settingsForm.BringToFront();
                    _settingsForm.Activate();
                }

                return;
            }

            var settingsManagerLogger = _serviceProvider.GetRequiredService<ILogger<SettingsManager>>();
            var settingsManager = new SettingsManager(_configuration, settingsManagerLogger);
            _settingsForm = new SettingsForm(
                _configuration,
                (url, id, player) => settingsManager.SaveSettings(url, id, player));

            // Handle form closing to clear the reference
            _settingsForm.FormClosed += (s, args) =>
            {
                _logger.LogInformation("Settings form closed");
                if (_settingsForm != null)
                {
                    _settingsForm.Dispose();
                    _settingsForm = null;
                }
            };

            var result = _settingsForm.ShowDialog();

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

        // Exit the application message loop first
        try
        {
            _logger.LogInformation("Exiting application message loop");
            Application.Exit();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exiting application message loop");
        }

        // Then stop the application host
        _lifetime.StopApplication();
    }

    private void OnConnectionStateChanged(object? sender, bool isConnected)
    {
        UpdateConnectionStatus(isConnected);
    }

    private void UpdateConnectionStatus(bool isConnected)
    {
        // Update the connection status menu item and icon on the UI thread
        if (_connectionStatusMenuItem != null && _notifyIcon != null)
        {
            // Use BeginInvoke to marshal to the UI thread if needed
            if (_contextMenu?.InvokeRequired == true)
            {
                _contextMenu.BeginInvoke(new Action(() =>
                {
                    _connectionStatusMenuItem.Text = isConnected
                        ? "Connection: Connected"
                        : "Connection: Error";

                    UpdateTrayIcon(isConnected);

                    _logger.LogInformation("Connection status changed to: {Status}",
                        isConnected ? "Connected" : "Error");
                }));
            }
            else
            {
                _connectionStatusMenuItem.Text = isConnected
                    ? "Connection: Connected"
                    : "Connection: Error";

                UpdateTrayIcon(isConnected);

                _logger.LogInformation("Connection status set to: {Status}",
                    isConnected ? "Connected" : "Error");
            }
        }
    }

    private void UpdateTrayIcon(bool isConnected)
    {
        if (_notifyIcon == null)
        {
            return;
        }

        try
        {
            // Determine which icon to use based on connection state
            var iconPath = isConnected
                ? Path.Combine(AppContext.BaseDirectory, "app.ico")
                : Path.Combine(AppContext.BaseDirectory, "app-offline.ico");

            var pngPath = isConnected
                ? Path.Combine(AppContext.BaseDirectory, "app.png")
                : Path.Combine(AppContext.BaseDirectory, "app-offline.png");

            // Dispose previous icon if it's not a system icon
            if (_notifyIcon.Icon != null && _notifyIcon.Icon != System.Drawing.SystemIcons.Application)
            {
                try
                {
                    var oldIcon = _notifyIcon.Icon;
                    _notifyIcon.Icon = null; // Clear reference before disposing
                    oldIcon.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error disposing previous icon");
                }
            }

            // Try to load the appropriate icon
            if (File.Exists(iconPath))
            {
                try
                {
                    _notifyIcon.Icon = new System.Drawing.Icon(iconPath);
                    _logger.LogInformation("Loaded {IconType} icon from {IconPath}",
                        isConnected ? "connected" : "offline", iconPath);
                }
                catch
                {
                    // If ICO fails, try PNG
                    if (File.Exists(pngPath))
                    {
                        using var bitmap = new System.Drawing.Bitmap(pngPath);
                        var iconHandle = bitmap.GetHicon();
                        _notifyIcon.Icon = System.Drawing.Icon.FromHandle(iconHandle);
                        _logger.LogInformation("Loaded {IconType} icon from {PngPath}",
                            isConnected ? "connected" : "offline", pngPath);
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
                _logger.LogInformation("Loaded {IconType} icon from {PngPath}",
                    isConnected ? "connected" : "offline", pngPath);
            }
            else
            {
                _logger.LogWarning("{IconType} icon file not found at {IconPath} or {PngPath}. Using default icon.",
                    isConnected ? "Connected" : "Offline", iconPath, pngPath);
                _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load {IconType} icon. Using default icon.",
                isConnected ? "connected" : "offline");
            _notifyIcon.Icon = System.Drawing.SystemIcons.Application;
        }
    }

    public override void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (_healthCheckService != null)
        {
            _healthCheckService.ConnectionStateChanged -= OnConnectionStateChanged;
        }

        // Dispose settings form if it's still open
        if (_settingsForm != null && !_settingsForm.IsDisposed)
        {
            try
            {
                _settingsForm.Close();
                _settingsForm.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error disposing settings form");
            }
        }

        // Dispose status dialog if it's still open
        if (_statusDialog != null && !_statusDialog.IsDisposed)
        {
            try
            {
                _statusDialog.Close();
                _statusDialog.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error disposing status dialog");
            }
        }

        if (_notifyIcon != null)
        {
            try
            {
                _notifyIcon.Visible = false;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error hiding notify icon during disposal");
            }

            try
            {
                _notifyIcon.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error disposing notify icon");
            }
        }

        try
        {
            _contextMenu?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error disposing context menu");
        }

        base.Dispose();
    }
}
