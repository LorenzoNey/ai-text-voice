using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using WisprClone.Infrastructure;
using WisprClone.ViewModels;

namespace WisprClone.Views;

/// <summary>
/// Interaction logic for OverlayWindow.xaml
/// </summary>
public partial class OverlayWindow : Window
{
    public OverlayWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closing += OnClosing;
        MouseEnter += OnMouseEnter;
        MouseLeave += OnMouseLeave;
    }

    private void OnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (DataContext is OverlayViewModel viewModel)
        {
            viewModel.IsMouseOverWindow = true;
        }
    }

    private void OnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (DataContext is OverlayViewModel viewModel)
        {
            viewModel.IsMouseOverWindow = false;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Make window not activatable and hidden from Alt+Tab
        var hwnd = new WindowInteropHelper(this).Handle;
        var extendedStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
        NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE,
            extendedStyle | NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE);
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Save position before closing
        if (DataContext is OverlayViewModel viewModel)
        {
            viewModel.SavePosition();
        }
    }

    private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
        {
            DragMove();

            // Update position in view model after drag
            if (DataContext is OverlayViewModel viewModel)
            {
                viewModel.WindowLeft = Left;
                viewModel.WindowTop = Top;
            }
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        // Hide instead of close
        Hide();

        if (DataContext is OverlayViewModel viewModel)
        {
            viewModel.SavePosition();
            viewModel.Hide();
        }
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is OverlayViewModel viewModel)
        {
            var text = viewModel.TranscriptionText;
            if (!string.IsNullOrWhiteSpace(text) &&
                text != "Press Ctrl+Ctrl to start..." &&
                text != "Listening..." &&
                text != "No speech detected")
            {
                try
                {
                    Clipboard.SetText(text);
                }
                catch
                {
                    // Clipboard access can fail sometimes, ignore
                }
            }
        }
    }

    private void ComboBox_DropDownOpened(object sender, EventArgs e)
    {
        if (DataContext is OverlayViewModel viewModel)
        {
            viewModel.IsDropdownOpen = true;
        }
    }

    private void ComboBox_DropDownClosed(object sender, EventArgs e)
    {
        if (DataContext is OverlayViewModel viewModel)
        {
            viewModel.IsDropdownOpen = false;
        }
    }
}
