using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using WisprClone.Core;
using WisprClone.Services.Interfaces;

namespace WisprClone.Views;

/// <summary>
/// Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly ISettingsService _settingsService;

    public SettingsWindow(ISettingsService settingsService)
    {
        InitializeComponent();
        _settingsService = settingsService;
        LoadSettings();
    }

    private void LoadSettings()
    {
        var settings = _settingsService.Current;

        // Speech provider
        foreach (ComboBoxItem item in SpeechProviderComboBox.Items)
        {
            if (item.Tag?.ToString() == settings.SpeechProvider.ToString())
            {
                SpeechProviderComboBox.SelectedItem = item;
                break;
            }
        }

        // Azure settings
        AzureKeyTextBox.Text = settings.AzureSubscriptionKey;
        AzureRegionTextBox.Text = settings.AzureRegion;
        UseAzureFallbackCheckBox.IsChecked = settings.UseAzureFallback;

        // OpenAI settings
        OpenAIKeyTextBox.Text = settings.OpenAIApiKey;

        // Language
        foreach (ComboBoxItem item in LanguageComboBox.Items)
        {
            if (item.Tag?.ToString() == settings.RecognitionLanguage)
            {
                LanguageComboBox.SelectedItem = item;
                break;
            }
        }

        // Hotkey settings
        DoubleTapIntervalTextBox.Text = settings.DoubleTapIntervalMs.ToString();
        MaxKeyHoldTextBox.Text = settings.MaxKeyHoldDurationMs.ToString();

        // Recording limits
        MaxRecordingDurationTextBox.Text = settings.MaxRecordingDurationSeconds.ToString();

        // Behavior settings
        AutoCopyCheckBox.IsChecked = settings.AutoCopyToClipboard;
        StartMinimizedCheckBox.IsChecked = settings.StartMinimized;
        MinimizeToTrayCheckBox.IsChecked = settings.MinimizeToTray;

        // Debugging
        EnableLoggingCheckBox.IsChecked = settings.EnableLogging;

        // About - set version from assembly
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = version != null ? $"v{version.Major}.{version.Minor}.{version.Build}" : "v1.0.0";
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        _settingsService.Update(settings =>
        {
            // Speech provider
            if (SpeechProviderComboBox.SelectedItem is ComboBoxItem providerItem)
            {
                var providerTag = providerItem.Tag?.ToString() ?? "Offline";
                settings.SpeechProvider = Enum.TryParse<SpeechProvider>(providerTag, out var provider)
                    ? provider
                    : SpeechProvider.Offline;
            }

            // Azure settings
            settings.AzureSubscriptionKey = AzureKeyTextBox.Text.Trim();
            settings.AzureRegion = AzureRegionTextBox.Text.Trim();
            settings.UseAzureFallback = UseAzureFallbackCheckBox.IsChecked ?? false;

            // OpenAI settings
            settings.OpenAIApiKey = OpenAIKeyTextBox.Text.Trim();

            // Language
            if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                settings.RecognitionLanguage = selectedItem.Tag?.ToString() ?? "en-US";
            }

            // Hotkey settings
            if (int.TryParse(DoubleTapIntervalTextBox.Text, out var interval))
            {
                settings.DoubleTapIntervalMs = Math.Clamp(interval, 100, 1000);
            }

            if (int.TryParse(MaxKeyHoldTextBox.Text, out var maxHold))
            {
                settings.MaxKeyHoldDurationMs = Math.Clamp(maxHold, 50, 500);
            }

            // Recording limits
            if (int.TryParse(MaxRecordingDurationTextBox.Text, out var maxDuration))
            {
                settings.MaxRecordingDurationSeconds = Math.Clamp(maxDuration,
                    Constants.MinMaxRecordingDurationSeconds,
                    Constants.MaxMaxRecordingDurationSeconds);
            }

            // Behavior settings
            settings.AutoCopyToClipboard = AutoCopyCheckBox.IsChecked ?? true;
            settings.StartMinimized = StartMinimizedCheckBox.IsChecked ?? false;
            settings.MinimizeToTray = MinimizeToTrayCheckBox.IsChecked ?? true;

            // Debugging
            settings.EnableLogging = EnableLoggingCheckBox.IsChecked ?? false;
        });

        MessageBox.Show(
            "Settings saved successfully.",
            "Settings Saved",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
