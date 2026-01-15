using Avalonia.Controls;
using Avalonia.Interactivity;
using WisprClone.Core;
using WisprClone.Services.Interfaces;

namespace WisprClone.Views;

/// <summary>
/// Settings window for configuring the application (Avalonia version).
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly ISettingsService _settingsService;

    public SettingsWindow()
    {
        InitializeComponent();
        _settingsService = null!; // Will be set via property
    }

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
        SelectComboBoxItemByTag(SpeechProviderComboBox, settings.SpeechProvider.ToString());

        // Azure settings
        AzureKeyTextBox.Text = settings.AzureSubscriptionKey;
        AzureRegionTextBox.Text = settings.AzureRegion;
        UseAzureFallbackCheckBox.IsChecked = settings.UseAzureFallback;

        // OpenAI settings
        OpenAIKeyTextBox.Text = settings.OpenAIApiKey;

        // Language
        SelectComboBoxItemByTag(LanguageComboBox, settings.RecognitionLanguage);

        // Hotkey settings
        DoubleTapIntervalTextBox.Text = settings.DoubleTapIntervalMs.ToString();
        MaxKeyHoldTextBox.Text = settings.MaxKeyHoldDurationMs.ToString();

        // Recording limits
        MaxRecordingDurationTextBox.Text = settings.MaxRecordingDurationSeconds.ToString();

        // Behavior
        AutoCopyCheckBox.IsChecked = settings.AutoCopyToClipboard;
        StartMinimizedCheckBox.IsChecked = settings.StartMinimized;
        MinimizeToTrayCheckBox.IsChecked = settings.MinimizeToTray;

        // Debugging
        EnableLoggingCheckBox.IsChecked = settings.EnableLogging;

        // Update offline provider visibility based on platform
        UpdateProviderVisibility();
    }

    private void UpdateProviderVisibility()
    {
        // On non-Windows platforms, hide the offline option
        if (!OperatingSystem.IsWindows())
        {
            foreach (var item in SpeechProviderComboBox.Items.Cast<ComboBoxItem>())
            {
                if (item.Tag?.ToString() == "Offline")
                {
                    item.IsVisible = false;
                    break;
                }
            }

            // If Offline was selected, switch to Azure
            var selectedItem = SpeechProviderComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem?.Tag?.ToString() == "Offline")
            {
                SelectComboBoxItemByTag(SpeechProviderComboBox, "Azure");
            }

            // Update the info text
            ProviderInfoText.Text = "Note: Offline speech recognition is only available on Windows. " +
                                    "On macOS and Linux, use Azure or OpenAI cloud services.";
        }
    }

    private static void SelectComboBoxItemByTag(ComboBox comboBox, string tag)
    {
        foreach (var item in comboBox.Items.Cast<ComboBoxItem>())
        {
            if (item.Tag?.ToString() == tag)
            {
                comboBox.SelectedItem = item;
                break;
            }
        }
    }

    private static string? GetSelectedComboBoxTag(ComboBox comboBox)
    {
        return (comboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        if (!ValidateInputs())
            return;

        _settingsService.Update(settings =>
        {
            // Speech provider
            var providerTag = GetSelectedComboBoxTag(SpeechProviderComboBox);
            if (Enum.TryParse<SpeechProvider>(providerTag, out var provider))
            {
                settings.SpeechProvider = provider;
            }

            // Azure settings
            settings.AzureSubscriptionKey = AzureKeyTextBox.Text ?? string.Empty;
            settings.AzureRegion = AzureRegionTextBox.Text ?? string.Empty;
            settings.UseAzureFallback = UseAzureFallbackCheckBox.IsChecked ?? false;

            // OpenAI settings
            settings.OpenAIApiKey = OpenAIKeyTextBox.Text ?? string.Empty;

            // Language
            settings.RecognitionLanguage = GetSelectedComboBoxTag(LanguageComboBox) ?? "en-US";

            // Hotkey settings
            if (int.TryParse(DoubleTapIntervalTextBox.Text, out var doubleTapInterval))
            {
                settings.DoubleTapIntervalMs = Math.Clamp(doubleTapInterval, 100, 1000);
            }
            if (int.TryParse(MaxKeyHoldTextBox.Text, out var maxKeyHold))
            {
                settings.MaxKeyHoldDurationMs = Math.Clamp(maxKeyHold, 50, 500);
            }

            // Recording limits
            if (int.TryParse(MaxRecordingDurationTextBox.Text, out var maxDuration))
            {
                settings.MaxRecordingDurationSeconds = Math.Clamp(maxDuration, 10, 600);
            }

            // Behavior
            settings.AutoCopyToClipboard = AutoCopyCheckBox.IsChecked ?? true;
            settings.StartMinimized = StartMinimizedCheckBox.IsChecked ?? false;
            settings.MinimizeToTray = MinimizeToTrayCheckBox.IsChecked ?? true;

            // Debugging
            settings.EnableLogging = EnableLoggingCheckBox.IsChecked ?? false;
        });

        Close();
    }

    private bool ValidateInputs()
    {
        // Validate numeric inputs
        if (!int.TryParse(DoubleTapIntervalTextBox.Text, out var doubleTapInterval) ||
            doubleTapInterval < 100 || doubleTapInterval > 1000)
        {
            DoubleTapIntervalTextBox.Focus();
            return false;
        }

        if (!int.TryParse(MaxKeyHoldTextBox.Text, out var maxKeyHold) ||
            maxKeyHold < 50 || maxKeyHold > 500)
        {
            MaxKeyHoldTextBox.Focus();
            return false;
        }

        if (!int.TryParse(MaxRecordingDurationTextBox.Text, out var maxDuration) ||
            maxDuration < 10 || maxDuration > 600)
        {
            MaxRecordingDurationTextBox.Focus();
            return false;
        }

        return true;
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
