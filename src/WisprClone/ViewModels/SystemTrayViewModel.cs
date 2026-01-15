using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using WisprClone.Core;
using WisprClone.Services.Interfaces;
using WisprClone.ViewModels.Base;

namespace WisprClone.ViewModels;

/// <summary>
/// ViewModel for the system tray icon.
/// </summary>
public partial class SystemTrayViewModel : ViewModelBase
{
    private readonly MainViewModel _mainViewModel;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string _tooltipText = "WisprClone - Ready";

    [ObservableProperty]
    private string _iconSource = "pack://application:,,,/Resources/Icons/tray_idle.ico";

    public SystemTrayViewModel(MainViewModel mainViewModel, ISettingsService settingsService)
    {
        _mainViewModel = mainViewModel;
        _settingsService = settingsService;

        _mainViewModel.StateChanged += OnMainStateChanged;
    }

    private void OnMainStateChanged(object? sender, TranscriptionState state)
    {
        TooltipText = state switch
        {
            TranscriptionState.Idle => "WisprClone - Ready (Ctrl+Ctrl to start)",
            TranscriptionState.Initializing => "WisprClone - Initializing...",
            TranscriptionState.Listening => "WisprClone - Listening...",
            TranscriptionState.Processing => "WisprClone - Processing...",
            TranscriptionState.CopyingToClipboard => "WisprClone - Copied to clipboard!",
            TranscriptionState.Error => "WisprClone - Error occurred",
            _ => "WisprClone"
        };

        IconSource = state switch
        {
            TranscriptionState.Listening => "pack://application:,,,/Resources/Icons/tray_listening.ico",
            TranscriptionState.Processing => "pack://application:,,,/Resources/Icons/tray_processing.ico",
            TranscriptionState.Error => "pack://application:,,,/Resources/Icons/tray_error.ico",
            _ => "pack://application:,,,/Resources/Icons/tray_idle.ico"
        };
    }

    [RelayCommand]
    private void ShowOverlay()
    {
        _mainViewModel.ShowOverlay();
    }

    [RelayCommand]
    private void HideOverlay()
    {
        _mainViewModel.HideOverlay();
    }

    [RelayCommand]
    private void ToggleOverlay()
    {
        _mainViewModel.ToggleOverlay();
    }

    [RelayCommand]
    private void OpenSettings()
    {
        _mainViewModel.OpenSettings();
    }

    [RelayCommand]
    private void Exit()
    {
        Application.Current.Shutdown();
    }
}
