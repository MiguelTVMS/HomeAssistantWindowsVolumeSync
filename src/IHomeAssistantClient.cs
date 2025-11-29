namespace HomeAssistantWindowsVolumeSync;

/// <summary>
/// Interface for sending volume updates to Home Assistant.
/// </summary>
public interface IHomeAssistantClient
{
    /// <summary>
    /// Sends a volume update to Home Assistant via webhook.
    /// </summary>
    /// <param name="volumePercent">Volume level from 0 to 100.</param>
    /// <param name="isMuted">Whether the audio is muted.</param>
    /// <returns>A task that completes when the update is sent.</returns>
    Task SendVolumeUpdateAsync(int volumePercent, bool isMuted);

    /// <summary>
    /// Performs a health check to verify connection to Home Assistant.
    /// Optionally sends current volume state to keep Home Assistant updated.
    /// </summary>
    /// <param name="volumePercent">Optional volume level from 0 to 100.</param>
    /// <param name="isMuted">Optional mute state.</param>
    /// <returns>True if the connection is healthy, false otherwise.</returns>
    Task<bool> CheckHealthAsync(int? volumePercent = null, bool? isMuted = null);
}
