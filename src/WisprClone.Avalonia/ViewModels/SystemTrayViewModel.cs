using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WisprClone.ViewModels.Base;

namespace WisprClone.ViewModels;

/// <summary>
/// ViewModel for system tray interactions (Avalonia version).
/// </summary>
public partial class SystemTrayViewModel : ViewModelBase
{
    private readonly MainViewModel _mainViewModel;

    public SystemTrayViewModel(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }

    [RelayCommand]
    public void ShowOverlay()
    {
        _mainViewModel.ShowOverlay();
    }

    [RelayCommand]
    public void HideOverlay()
    {
        _mainViewModel.HideOverlay();
    }

    [RelayCommand]
    public void ToggleOverlay()
    {
        _mainViewModel.ToggleOverlay();
    }

    [RelayCommand]
    public void OpenSettings()
    {
        _mainViewModel.OpenSettings();
    }
}
