using System.Windows.Forms;

namespace HomeAssistantWindowsVolumeSync;

/// <summary>
/// Form for editing application settings
/// </summary>
public class SettingsForm : Form
{
    private readonly IAppConfiguration _configuration;
    private readonly Action<string, string, string> _saveSettings;

    private static readonly System.Drawing.Font _labelFont = new System.Drawing.Font("Segoe UI", 9F);
    private static readonly System.Drawing.Font _textBoxFont = new System.Drawing.Font("Segoe UI", 9F);
    private static readonly System.Drawing.Font _buttonFont = new System.Drawing.Font("Segoe UI", 9F);
    private static readonly System.Drawing.Font _boldButtonFont = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);

    private TextBox _webhookUrlTextBox = null!;
    private TextBox _webhookIdTextBox = null!;
    private TextBox _targetMediaPlayerTextBox = null!;
    private CheckBox _runOnStartupCheckBox = null!;
    private Button _saveButton = null!;
    private Button _cancelButton = null!;
    private Button _helpButton = null!;
    private Label _webhookUrlLabel = null!;
    private Label _webhookIdLabel = null!;
    private Label _targetMediaPlayerLabel = null!;
    private ToolTip _toolTip = null!;

    public SettingsForm(IAppConfiguration configuration, Action<string, string, string> saveSettings)
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
        Height = 330;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        // Create ToolTip
        _toolTip = new ToolTip
        {
            AutoPopDelay = 10000,
            InitialDelay = 500,
            ReshowDelay = 100
        };

        // Webhook URL Label
        _webhookUrlLabel = new Label
        {
            Text = "Home Assistant URL:",
            Left = 20,
            Top = 20,
            Width = 200,
            Font = _labelFont
        };
        Controls.Add(_webhookUrlLabel);

        // Webhook URL TextBox
        _webhookUrlTextBox = new TextBox
        {
            Left = 20,
            Top = 45,
            Width = 540,
            Font = _textBoxFont,
            PlaceholderText = "https://your-home-assistant-url"
        };
        Controls.Add(_webhookUrlTextBox);

        // Webhook ID Label
        _webhookIdLabel = new Label
        {
            Text = "Webhook ID:",
            Left = 20,
            Top = 80,
            Width = 200,
            Font = _labelFont
        };
        Controls.Add(_webhookIdLabel);

        // Webhook ID TextBox
        _webhookIdTextBox = new TextBox
        {
            Left = 20,
            Top = 105,
            Width = 540,
            Font = _textBoxFont,
            PlaceholderText = "homeassistant_windows_volume_sync"
        };
        _toolTip.SetToolTip(_webhookIdTextBox,
            "The webhook automation for Home Assistant installation is available at:\n" +
            "https://github.com/MiguelTVMS/HomeAssistantWindowsVolumeSync/blob/main/home-assistant/INSTALL.md");
        Controls.Add(_webhookIdTextBox);

        // Target Media Player Label
        _targetMediaPlayerLabel = new Label
        {
            Text = "Target Media Player Entity ID:",
            Left = 20,
            Top = 140,
            Width = 200,
            Font = _labelFont
        };
        Controls.Add(_targetMediaPlayerLabel);

        // Target Media Player TextBox
        _targetMediaPlayerTextBox = new TextBox
        {
            Left = 20,
            Top = 165,
            Width = 540,
            Font = _textBoxFont,
            PlaceholderText = "media_player.your_media_player"
        };
        Controls.Add(_targetMediaPlayerTextBox);

        // Run on Startup CheckBox
        _runOnStartupCheckBox = new CheckBox
        {
            Text = "Run when Windows starts",
            Left = 20,
            Top = 200,
            Width = 540,
            Font = _textBoxFont
        };
        Controls.Add(_runOnStartupCheckBox);

        // Create button panel at bottom with Windows standard layout (matching status screen)
        var buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.RightToLeft,
            Dock = DockStyle.Bottom,
            Width = ClientSize.Width,
            Padding = new Padding(0, 0, 20, 10), // Standard button panel padding
            AutoSize = true,
            WrapContents = false,
        };
        Controls.Add(buttonPanel);

        // Help Button (leftmost, with same padding as right buttons)
        _helpButton = new Button
        {
            Text = "?",
            AutoSize = false,
            Width = 30,
            Height = 30,
            Font = _boldButtonFont,
        };
        _helpButton.Click += OnHelpClick;
        _toolTip.SetToolTip(_helpButton, "Home Assistant Installation Instructions");
        buttonPanel.Controls.Add(_helpButton);

        // Cancel Button (rightmost, standard Windows size)
        _cancelButton = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new System.Drawing.Size(75, 30),
            Font = _buttonFont
        };
        buttonPanel.Controls.Add(_cancelButton);

        // Save Button (standard Windows size, 6px gap)
        _saveButton = new Button
        {
            Text = "Save",
            DialogResult = DialogResult.OK,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new System.Drawing.Size(75, 30),
            Font = _buttonFont
        };
        _saveButton.Click += OnSaveClick;
        buttonPanel.Controls.Add(_saveButton);

        AcceptButton = _saveButton;
        CancelButton = _cancelButton;
    }

    private void LoadSettings()
    {
        _webhookUrlTextBox.Text = _configuration.WebhookUrl ?? "";
        _webhookIdTextBox.Text = _configuration.WebhookId ?? "";
        _targetMediaPlayerTextBox.Text = _configuration.TargetMediaPlayer ?? "";
        _runOnStartupCheckBox.Checked = WindowsStartupManager.IsStartupEnabled();
    }

    private void OnSaveClick(object? sender, EventArgs e)
    {
        var webhookUrl = _webhookUrlTextBox.Text.Trim();
        var webhookId = _webhookIdTextBox.Text.Trim();
        var targetMediaPlayer = _targetMediaPlayerTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            MessageBox.Show(
                "Home Assistant URL cannot be empty.",
                "Validation Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        if (string.IsNullOrWhiteSpace(webhookId))
        {
            MessageBox.Show(
                "Webhook ID cannot be empty.",
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
            _saveSettings(webhookUrl, webhookId, targetMediaPlayer);

            // Handle Windows startup setting
            try
            {
                if (_runOnStartupCheckBox.Checked)
                {
                    WindowsStartupManager.EnableStartup();
                }
                else
                {
                    WindowsStartupManager.DisableStartup();
                }
            }
            catch (Exception startupEx)
            {
                MessageBox.Show(
                    $"Settings saved, but failed to update startup configuration:\n{startupEx.Message}",
                    "Startup Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

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

    private void OnHelpClick(object? sender, EventArgs e)
    {
        const string url = "https://github.com/MiguelTVMS/HomeAssistantWindowsVolumeSync/blob/main/home-assistant/INSTALL.md";

        var result = MessageBox.Show(
            "Home Assistant Webhook Automation Installation Instructions\n\n" +
            "To complete the setup, you need to configure a webhook automation in Home Assistant.\n\n" +
            "The instructions include:\n" +
            "• How to create the webhook automation\n" +
            "• Configuration examples\n" +
            "• Troubleshooting tips\n\n" +
            "Would you like to open the installation guide in your browser?",
            "Home Assistant Installation Help",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information,
            MessageBoxDefaultButton.Button2);

        if (result == DialogResult.Yes)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to open browser:\n{ex.Message}\n\nPlease visit:\n{url}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
