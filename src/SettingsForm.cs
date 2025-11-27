using Microsoft.Extensions.Configuration;
using System.Windows.Forms;

namespace HomeAssistantWindowsVolumeSync;

/// <summary>
/// Form for editing application settings
/// </summary>
public class SettingsForm : Form
{
    private readonly IConfiguration _configuration;
    private readonly Action<string, string> _saveSettings;

    private TextBox _webhookUrlTextBox = null!;
    private TextBox _targetMediaPlayerTextBox = null!;
    private Button _saveButton = null!;
    private Button _cancelButton = null!;
    private Label _webhookUrlLabel = null!;
    private Label _targetMediaPlayerLabel = null!;

    public SettingsForm(IConfiguration configuration, Action<string, string> saveSettings)
    {
        _configuration = configuration;
        _saveSettings = saveSettings;
        InitializeComponents();
        LoadSettings();
    }

    private void InitializeComponents()
    {
        // Form settings
        Text = "Home Assistant Volume Sync - Settings";
        Width = 600;
        Height = 250;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        // Webhook URL Label
        _webhookUrlLabel = new Label
        {
            Text = "Home Assistant Webhook URL:",
            Left = 20,
            Top = 20,
            Width = 200
        };
        Controls.Add(_webhookUrlLabel);

        // Webhook URL TextBox
        _webhookUrlTextBox = new TextBox
        {
            Left = 20,
            Top = 45,
            Width = 540,
            Font = new System.Drawing.Font("Segoe UI", 9F)
        };
        Controls.Add(_webhookUrlTextBox);

        // Target Media Player Label
        _targetMediaPlayerLabel = new Label
        {
            Text = "Target Media Player Entity ID:",
            Left = 20,
            Top = 80,
            Width = 200
        };
        Controls.Add(_targetMediaPlayerLabel);

        // Target Media Player TextBox
        _targetMediaPlayerTextBox = new TextBox
        {
            Left = 20,
            Top = 105,
            Width = 540,
            Font = new System.Drawing.Font("Segoe UI", 9F)
        };
        Controls.Add(_targetMediaPlayerTextBox);

        // Save Button
        _saveButton = new Button
        {
            Text = "Save",
            Left = 380,
            Top = 160,
            Width = 80,
            DialogResult = DialogResult.OK
        };
        _saveButton.Click += OnSaveClick;
        Controls.Add(_saveButton);

        // Cancel Button
        _cancelButton = new Button
        {
            Text = "Cancel",
            Left = 480,
            Top = 160,
            Width = 80,
            DialogResult = DialogResult.Cancel
        };
        Controls.Add(_cancelButton);

        AcceptButton = _saveButton;
        CancelButton = _cancelButton;
    }

    private void LoadSettings()
    {
        _webhookUrlTextBox.Text = _configuration["HomeAssistant:WebhookUrl"] ?? "";
        _targetMediaPlayerTextBox.Text = _configuration["HomeAssistant:TargetMediaPlayer"] ?? "";
    }

    private void OnSaveClick(object? sender, EventArgs e)
    {
        var webhookUrl = _webhookUrlTextBox.Text.Trim();
        var targetMediaPlayer = _targetMediaPlayerTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            MessageBox.Show(
                "Webhook URL cannot be empty.",
                "Validation Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        if (string.IsNullOrWhiteSpace(targetMediaPlayer))
        {
            MessageBox.Show(
                "Target Media Player cannot be empty.",
                "Validation Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        try
        {
            _saveSettings(webhookUrl, targetMediaPlayer);
            MessageBox.Show(
                "Settings saved successfully!\n\nThe application will use the new settings for future volume updates.",
                "Settings Saved",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to save settings:\n{ex.Message}",
                "Save Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            DialogResult = DialogResult.None;
        }
    }
}
