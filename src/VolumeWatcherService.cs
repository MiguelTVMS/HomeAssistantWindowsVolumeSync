using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;

namespace HomeAssistantWindowsVolumeSync;

/// <summary>
/// Background service that monitors Windows system volume changes
/// and sends updates to Home Assistant via webhook.
/// </summary>
public class VolumeWatcherService : BackgroundService
{
    private readonly ILogger<VolumeWatcherService> _logger;
    private readonly IHomeAssistantClient _homeAssistantClient;
    private readonly IAppConfiguration _configuration;
    private MMDeviceEnumerator? _deviceEnumerator;
    private MMDevice? _defaultDevice;
    private AudioEndpointVolumeNotificationDelegate? _volumeDelegate;
    private volatile bool _isPaused;
    private CancellationTokenSource? _debounceCts;
    private float _lastVolumeScalar;
    private bool _lastMuteState;
    private readonly object _debounceLock = new object();

    public VolumeWatcherService(
        ILogger<VolumeWatcherService> logger,
        IHomeAssistantClient homeAssistantClient,
        IAppConfiguration configuration)
    {
        _logger = logger;
        _homeAssistantClient = homeAssistantClient;
        _configuration = configuration;
    }

    /// <summary>
    /// Sets the pause state of the volume watcher.
    /// </summary>
    public void SetPaused(bool isPaused)
    {
        _isPaused = isPaused;
        _logger.LogInformation("Volume watcher {Status}", isPaused ? "paused" : "resumed");
    }

    /// <summary>
    /// Gets the current volume state.
    /// </summary>
    /// <returns>Tuple containing (volume percent, is muted), or null if no audio device available.</returns>
    public virtual (int volumePercent, bool isMuted)? GetCurrentVolumeState()
    {
        if (_defaultDevice?.AudioEndpointVolume == null)
        {
            return null;
        }

        try
        {
            var volumeScalar = _defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar;
            var isMuted = _defaultDevice.AudioEndpointVolume.Mute;
            var volumePercent = (int)Math.Round(volumeScalar * 100);
            return (volumePercent, isMuted);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get current volume state");
            return null;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("VolumeWatcherService is starting...");

        try
        {
            // Initialize the device enumerator and get the default audio endpoint
            _deviceEnumerator = new MMDeviceEnumerator();

            try
            {
                _defaultDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            }
            catch (System.Runtime.InteropServices.COMException ex) when (ex.HResult == unchecked((int)0x80070490))
            {
                // Element not found - no audio device available
                _logger.LogWarning("No default audio endpoint found. Service will continue but volume monitoring is disabled.");
                _defaultDevice = null;
            }

            if (_defaultDevice?.AudioEndpointVolume != null)
            {
                // Create a delegate that handles volume change events
                _volumeDelegate = new AudioEndpointVolumeNotificationDelegate(OnVolumeNotification);
                _defaultDevice.AudioEndpointVolume.OnVolumeNotification += _volumeDelegate;

                _logger.LogInformation("Volume watcher initialized successfully. Listening for volume changes...");

                // Send initial volume state
                await SendVolumeUpdateAsync(
                    _defaultDevice.AudioEndpointVolume.MasterVolumeLevelScalar,
                    _defaultDevice.AudioEndpointVolume.Mute);
            }
            else
            {
                _logger.LogWarning("Could not access the default audio endpoint. Volume monitoring is disabled.");
            }

            // Keep the service running until cancellation is requested
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("VolumeWatcherService is stopping...");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in VolumeWatcherService");
            throw;
        }
    }

    private void OnVolumeNotification(AudioVolumeNotificationData data)
    {
        HandleVolumeChange(data.MasterVolume, data.Muted);
    }

    /// <summary>
    /// Handles a volume change event by debouncing and sending the update to Home Assistant.
    /// This method is internal to allow testing of the debounce logic.
    /// </summary>
    /// <param name="volumeScalar">The volume level as a scalar value (0.0 to 1.0).</param>
    /// <param name="isMuted">Whether the audio is muted.</param>
    internal void HandleVolumeChange(float volumeScalar, bool isMuted)
    {
        // Skip processing if paused
        if (_isPaused)
        {
            _logger.LogDebug("Volume change detected but skipped (paused): {Volume}%, Muted: {Muted}",
                volumeScalar * 100, isMuted);
            return;
        }

        _logger.LogDebug("Volume change detected: {Volume}%, Muted: {Muted}",
            volumeScalar * 100, isMuted);

        // Update the latest volume state and reset the debounce
        lock (_debounceLock)
        {
            _lastVolumeScalar = volumeScalar;
            _lastMuteState = isMuted;

            // Cancel and dispose any pending debounce task using local reference
            var previousCts = _debounceCts;
            previousCts?.Cancel();
            previousCts?.Dispose();

            _debounceCts = new CancellationTokenSource();
            var token = _debounceCts.Token;
            var waitTime = _configuration.DebounceTimer;

            // Start a new debounce task with exception handling
            _ = Task.Delay(waitTime, token).ContinueWith(t =>
            {
                if (!t.IsCanceled)
                {
                    try
                    {
                        SendDebouncedVolumeUpdate();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in debounced volume update");
                    }
                }
            }, TaskScheduler.Default);
        }
    }

    private void SendDebouncedVolumeUpdate()
    {
        float volumeScalar;
        bool isMuted;

        lock (_debounceLock)
        {
            volumeScalar = _lastVolumeScalar;
            isMuted = _lastMuteState;
        }

        // Fire and forget - we don't want to block
        _ = SendVolumeUpdateAsync(volumeScalar, isMuted);
    }

    private async Task SendVolumeUpdateAsync(float volumeScalar, bool isMuted)
    {
        try
        {
            // Convert to 0-100 scale as expected by the webhook
            var volumePercent = (int)Math.Round(volumeScalar * 100);

            await _homeAssistantClient.SendVolumeUpdateAsync(volumePercent, isMuted);

            _logger.LogDebug("Volume update sent: {Volume}%, Muted: {Muted}", volumePercent, isMuted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send volume update to Home Assistant");
        }
    }

    public override void Dispose()
    {
        if (_defaultDevice?.AudioEndpointVolume != null && _volumeDelegate != null)
        {
            _defaultDevice.AudioEndpointVolume.OnVolumeNotification -= _volumeDelegate;
        }

        try
        {
            _debounceCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // CancellationTokenSource was already disposed, ignore
        }

        _debounceCts?.Dispose();
        _defaultDevice?.Dispose();
        _deviceEnumerator?.Dispose();

        base.Dispose();
    }
}
