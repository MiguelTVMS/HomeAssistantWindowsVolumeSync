# Home Assistant Installation Guide

This guide will help you set up the webhook automation in Home Assistant to receive volume updates from the Windows Volume Sync application.

## Overview

The Windows Volume Sync application sends volume changes from your Windows PC to Home Assistant via webhooks. This requires creating an automation in Home Assistant that listens for webhook events and updates your media player accordingly.

## Understanding the Webhook

### What is a Webhook?

A webhook is an endpoint in Home Assistant that can receive HTTP POST requests from external applications. When the Windows Volume Sync application detects a volume change, it sends a JSON payload to your Home Assistant webhook URL.

### Webhook Components

The webhook URL consists of two parts:

1. **Base URL**: Your Home Assistant URL (e.g., `https://home.example.com`)
2. **Webhook ID**: A unique identifier for this automation (e.g., `homeassistant_windows_volume_sync`)

**Complete URL Example:**

```
https://home.example.com/api/webhook/homeassistant_windows_volume_sync
```

### Webhook ID

The **Webhook ID** is crucial - it connects your Windows application to the Home Assistant automation. You can use the default ID (`homeassistant_windows_volume_sync`) or create your own custom ID.

**Important:** The Webhook ID in your Home Assistant automation **must match** the Webhook ID configured in the Windows Volume Sync application settings.

### Payload Format

The application sends the following JSON payload:

```json
{
  "volume": 50,
  "mute": false,
  "target_media_player": "media_player.speaker"
}
```

- **volume**: Integer from 0-100 representing the Windows volume percentage
- **mute**: Boolean indicating if Windows is muted
- **target_media_player**: The Home Assistant entity ID of your media player (configured in Windows app settings)

## Step-by-Step Installation

### Step 1: Copy the Automation YAML

1. Download or copy the automation file: [`automation.yaml`](./automation.yaml)
2. Open the file and review the settings

### Step 2: Add the Automation to Home Assistant

You can add the automation using one of these methods:

#### Option A: Using the Home Assistant UI

1. Go to **Settings** → **Automations & Scenes**
2. Click the **+ CREATE AUTOMATION** button
3. Click **Skip** on the template selection
4. Click the **⋮** (three dots) in the top right
5. Select **Edit in YAML**
6. Paste the automation YAML content
7. Click **Save**
8. Give your automation a name (e.g., "Windows Volume Sync")

#### Option B: Using automations.yaml File

1. Open your Home Assistant configuration directory
2. Edit the `automations.yaml` file (or create it if it doesn't exist)
3. Paste the automation YAML content at the end of the file
4. Save the file
5. Restart Home Assistant or reload automations

### Step 3: Configure Windows Volume Sync

1. Open the **Windows Volume Sync** application settings (right-click system tray icon → Settings)
2. Configure the following fields:

   - **Home Assistant URL**: Your Home Assistant base URL
     - Example: `https://home.example.com`
     - Or use your Nabu Casa URL: `https://your-instance.ui.nabu.casa`
     - Or local IP: `http://192.168.1.100:8123`

   - **Webhook ID**: Must match the `webhook_id` in your Home Assistant automation
     - Default: `homeassistant_windows_volume_sync`
     - If you want to use a custom webhook ID, update both the automation file and this setting

   - **Target Media Player**: Your media player entity ID
     - Example: `media_player.speaker`
     - Find entity IDs in: **Developer Tools** → **States** in Home Assistant

3. Click **Save**

### Step 4: Test the Connection

1. Ensure the Windows Volume Sync application is running (check system tray)
2. Change your Windows volume using:
   - Volume keys on your keyboard
   - Volume slider in Windows
   - Volume knob if you have one
3. Check your Home Assistant media player - the volume should update automatically

## Troubleshooting

### Webhook Not Receiving Data

1. **Check the webhook URL format:**
   - Verify Base URL doesn't include the webhook path
   - Ensure Webhook ID matches between Windows app and Home Assistant

2. **Check Home Assistant logs:**
   - Go to **Settings** → **System** → **Logs**
   - Look for webhook-related errors

3. **Verify network connectivity:**
   - Ensure your PC can reach Home Assistant
   - Test the webhook URL manually using curl:

     ```bash
     curl -X POST "https://your-ha-url/api/webhook/homeassistant_windows_volume_sync" \
       -H "Content-Type: application/json" \
       -d '{"volume":50,"mute":false,"target_media_player":"media_player.test"}'
     ```

### Volume Not Changing on Media Player

1. **Verify entity ID:**
   - Check that `target_media_player` matches your actual media player
   - Go to **Developer Tools** → **States** to find the correct entity ID

2. **Check media player state:**
   - Ensure the media player is powered on
   - Some devices only accept volume changes when playing

3. **Test the automation manually:**
   - Go to **Developer Tools** → **Services**
   - Call `media_player.volume_set` with your entity ID
   - If this doesn't work, the issue is with the media player, not the automation

### SSL Certificate Errors

If you're using HTTPS with a self-signed certificate:

1. Set `StrictTLS` to `false` in `appsettings.json`
2. Or import your certificate into Windows Trusted Root Certificates

### Multiple Media Players

To control different media players, you can:

1. **Option A**: Set the `TargetMediaPlayer` in the Windows app settings to the desired media player
2. **Option B**: Create separate automations with different webhook IDs for each media player, then configure different Windows PCs to use different webhook IDs

## Advanced Configuration

### Filtering Volume Updates

To prevent excessive updates, you can add conditions to the automation:

```yaml
condition:
  - condition: template
    value_template: "{{ (volume | int) % 5 == 0 }}"  # Only update on multiples of 5
```

### Logging Webhook Calls

Add a notification action to debug webhook calls:

```yaml
action:
  - service: system_log.write
    data:
      message: "Windows volume changed: {{ volume }}%, mute: {{ mute }}"
      level: info
```

## Security Considerations

### Local Network Only

For maximum security, set the webhook to accept local traffic only:

```yaml
trigger:
  - platform: webhook
    webhook_id: homeassistant_windows_volume_sync
    local_only: true  # Only accept requests from local network
```

### Using HTTPS

Always use HTTPS when accessing Home Assistant over the internet:

- Enable SSL/TLS in Home Assistant
- Use Nabu Casa Cloud for easy secure access
- Or set up a reverse proxy with Let's Encrypt certificates

### Webhook Authentication

Home Assistant webhooks don't require authentication by default. To add security:

- Use `local_only: true` for local network access
- Use a long, random webhook ID
- Configure your firewall to restrict access

## Related Documentation

- [Home Assistant Webhook Trigger Documentation](https://www.home-assistant.io/docs/automation/trigger/#webhook-trigger)
- [Home Assistant Media Player Integration](https://www.home-assistant.io/integrations/media_player/)
- [Windows Volume Sync GitHub Repository](https://github.com/MiguelTVMS/HomeAssistantWindowsVolumeSync)

## Support

If you encounter issues:

1. Check the [GitHub Issues](https://github.com/MiguelTVMS/HomeAssistantWindowsVolumeSync/issues)
2. Review Home Assistant logs for webhook errors
3. Create a new issue with detailed information about your setup

## Example Scenarios

### Scenario 1: Single Speaker

```yaml
# In automation - uses default from automation.yaml
target_media_player: "{{ trigger.json.target_media_player | default('media_player.speaker') }}"

# In Windows app settings
Home Assistant URL: https://home.example.com
Webhook ID: homeassistant_windows_volume_sync
Target Media Player: media_player.speaker
```

### Scenario 2: Multiple Rooms

Create separate automations with different webhook IDs:

**Office Automation:**

```yaml
webhook_id: windows_volume_office
target_media_player: media_player.office_speaker
```

**Bedroom Automation:**

```yaml
webhook_id: windows_volume_bedroom
target_media_player: media_player.bedroom_speaker
```

Then configure different Windows PCs to use different webhook IDs.

### Scenario 3: Nabu Casa Cloud

```yaml
# Windows app settings
Home Assistant URL: https://your-instance.ui.nabu.casa
Webhook ID: homeassistant_windows_volume_sync
StrictTLS: true
```

No port forwarding or firewall changes needed!

---

**Need Help?** Open an issue on [GitHub](https://github.com/MiguelTVMS/HomeAssistantWindowsVolumeSync/issues) with your configuration (remove sensitive URLs/tokens).
